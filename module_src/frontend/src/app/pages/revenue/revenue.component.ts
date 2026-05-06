import { CommonModule } from '@angular/common';
import { HttpClient, HttpParams } from '@angular/common/http';
import { AfterViewInit, Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-revenue-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './revenue.component.html',
  styleUrl: './revenue.component.css',
})
export class RevenuePageComponent implements OnInit, AfterViewInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly apiTimeoutMs = 10_000;
  /** Số bucket mặc định khi user không chọn from/to (theo lịch Asia/Ho_Chi_Minh). */
  private readonly chartDefaultPeriodCount = 10;
  /** Zoom in tối đa: ít nhất bấy nhiêu điểm còn hiển thị. */
  private readonly chartZoomMinPoints = 3;
  private readonly chartLayout = { left: 56, rightPad: 20, top: 20, bottomPad: 44 } as const;
  @ViewChild('revenueChartCanvas') private revenueChartCanvas?: ElementRef<HTMLCanvasElement>;
  @ViewChild('productPickerDialog') private productPickerDialog?: ElementRef<HTMLDivElement>;
  private chartWheelCleanup?: () => void;

  buyers: Buyer[] = [];
  products: Product[] = [];
  bills: BillListItem[] = [];
  selectedBuyerId: number | null = null;
  selectedStatus: '' | 'pending' | 'completed' = '';
  fromDate = '';
  toDate = '';
  payableMaxVnd = 100_000_000;
  selectedProductIds: number[] = [];
  isProductPickerOpen = false;
  draftProductIds: number[] = [];
  private pickerOpenerEl: HTMLElement | null = null;
  private prevBodyOverflow: string | null = null;
  private chartTheme: ChartTheme | null = null;
  currentPage = 1;
  pageSize: 5 | 10 | 15 | 20 = 5;
  totalCount = 0;
  totalPages = 0;
  revenueMode: RevenueMode = 'total';
  revenueBucket: RevenueBucket = 'day';
  chartBuyerId: number | null = null;
  chartProductId: number | null = null;
  chartFromDate = '';
  chartToDate = '';
  /** Khi UI để trống from/to: 10 ngày/tuần/tháng gần nhất (VN); lưu để export/đối chiếu khớp. */
  private chartEffectiveRange: { from: string; to: string } | null = null;
  /** Toàn bộ chuỗi đã tải; zoom chỉ thay đổi cửa sổ hiển thị, không zoom ra ngoài độ dài này. */
  private chartDataFull: RevenuePoint[] = [];
  private chartViewStart = 0;
  private chartViewEnd = 0;
  revenuePoints: RevenuePoint[] = [];
  chartLoading = false;
  chartErrorMessage = '';
  compareMessage = '';
  compareErrorMessage = '';
  chartPlotPoints: ChartPlotPoint[] = [];
  hoverPoint: ChartPlotPoint | null = null;
  loading = false;
  errorMessage = '';

  get hasChartSeries(): boolean {
    return this.chartDataFull.length > 0;
  }

  get chartAriaLabel(): string {
    if (this.chartDataFull.length === 0) {
      return 'Biểu đồ doanh thu: chưa có dữ liệu theo bộ lọc hiện tại.';
    }
    const total = this.chartDataFull.reduce((sum, p) => sum + p.revenueVnd, 0);
    const bucketLabel =
      this.revenueBucket === 'day' ? 'ngày' : this.revenueBucket === 'week' ? 'tuần' : 'tháng';
    const first = this.chartDataFull[0]?.bucketStartLocal?.slice(0, 10) ?? '';
    const last = this.chartDataFull[this.chartDataFull.length - 1]?.bucketStartLocal?.slice(0, 10) ?? '';
    return `Biểu đồ doanh thu (${bucketLabel}): ${this.chartDataFull.length} điểm từ ${first} đến ${last}, tổng ${total.toLocaleString('vi-VN')} VND.`;
  }

  get selectedProductsLabel(): string {
    if (this.selectedProductIds.length === 0) {
      return 'Tất cả product';
    }
    return `Đã chọn ${this.selectedProductIds.length} product`;
  }

  ngOnInit(): void {
    void this.loadFiltersAndData();
  }

  ngAfterViewInit(): void {
    this.renderRevenueChart();
    const el = this.revenueChartCanvas?.nativeElement;
    if (el) {
      const fn = (e: WheelEvent) => this.onChartWheel(e);
      el.addEventListener('wheel', fn, { passive: false });
      this.chartWheelCleanup = () => el.removeEventListener('wheel', fn);
    }
  }

  ngOnDestroy(): void {
    this.chartWheelCleanup?.();
    if (this.isProductPickerOpen) {
      document.body.style.overflow = this.prevBodyOverflow ?? '';
      this.prevBodyOverflow = null;
      this.pickerOpenerEl = null;
    }
  }

  openProductPicker(): void {
    this.draftProductIds = [...this.selectedProductIds];
    this.isProductPickerOpen = true;
    this.pickerOpenerEl = (document.activeElement as HTMLElement | null) ?? null;
    this.prevBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    setTimeout(() => this.productPickerDialog?.nativeElement.focus(), 0);
  }

  cancelProductPicker(): void {
    this.isProductPickerOpen = false;
    this.draftProductIds = [];
    this.releaseProductPickerSideEffects();
  }

  /**
   * Tab cycle inside dialog so keyboard focus does not escape into background while modal is open.
   */
  onPickerKeydown(event: KeyboardEvent): void {
    if (event.key !== 'Tab') {
      return;
    }
    const dialog = this.productPickerDialog?.nativeElement;
    if (!dialog) {
      return;
    }
    const focusables = dialog.querySelectorAll<HTMLElement>(
      'a[href], button:not([disabled]), input:not([disabled]), select:not([disabled]), textarea:not([disabled]), [tabindex]:not([tabindex="-1"])',
    );
    if (focusables.length === 0) {
      event.preventDefault();
      dialog.focus();
      return;
    }
    const first = focusables[0];
    const last = focusables[focusables.length - 1];
    const active = document.activeElement as HTMLElement | null;
    if (event.shiftKey && (active === first || active === dialog)) {
      event.preventDefault();
      last.focus();
    } else if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
    }
  }

  private releaseProductPickerSideEffects(): void {
    document.body.style.overflow = this.prevBodyOverflow ?? '';
    this.prevBodyOverflow = null;
    const opener = this.pickerOpenerEl;
    this.pickerOpenerEl = null;
    if (opener && typeof opener.focus === 'function') {
      setTimeout(() => opener.focus(), 0);
    }
  }

  toggleProduct(productId: number, checked: boolean): void {
    if (checked) {
      if (!this.draftProductIds.includes(productId)) {
        this.draftProductIds = [...this.draftProductIds, productId];
      }
    } else {
      this.draftProductIds = this.draftProductIds.filter((x) => x !== productId);
    }
  }

  clearDraftProducts(): void {
    this.draftProductIds = [];
  }

  async applyProductPicker(): Promise<void> {
    this.selectedProductIds = [...this.draftProductIds];
    this.isProductPickerOpen = false;
    this.releaseProductPickerSideEffects();
    this.currentPage = 1;
    await this.loadBills();
  }

  async onFilterChanged(): Promise<void> {
    this.currentPage = 1;
    await this.loadBills();
  }

  async onChartFilterChanged(): Promise<void> {
    await this.loadRevenueSeries();
  }

  async onPageSizeChanged(): Promise<void> {
    this.currentPage = 1;
    await this.loadBills();
  }

  async previousPage(): Promise<void> {
    if (this.currentPage <= 1) {
      return;
    }
    this.currentPage -= 1;
    await this.loadBills();
  }

  async nextPage(): Promise<void> {
    if (this.currentPage >= this.totalPages) {
      return;
    }
    this.currentPage += 1;
    await this.loadBills();
  }

  async downloadCsv(): Promise<void> {
    const params = this.buildParams();
    const csv = await this.requestWithTimeout(
      this.http.get(`${this.apiBaseUrl}/api/bills/export.csv`, {
        params,
        responseType: 'blob',
      }),
    );

    const link = document.createElement('a');
    const url = URL.createObjectURL(csv);
    link.href = url;
    link.download = 'bills-export.csv';
    link.click();
    URL.revokeObjectURL(url);
  }

  async exportChartAsPng(): Promise<void> {
    this.renderRevenueChart();
    const canvas = this.revenueChartCanvas?.nativeElement;
    if (!canvas || this.chartDataFull.length === 0) {
      return;
    }

    const link = document.createElement('a');
    link.href = canvas.toDataURL('image/png');
    link.download = `revenue-chart-${this.revenueMode}-${this.revenueBucket}.png`;
    link.click();
  }

  async downloadNontechXlsx(): Promise<void> {
    const params = this.buildNontechExportParams();
    const blob = await this.requestWithTimeout(
      this.http.get(`${this.apiBaseUrl}/api/reports/export-nontech.xlsx`, {
        params,
        responseType: 'blob',
      }),
    );

    const link = document.createElement('a');
    const url = URL.createObjectURL(blob);
    link.href = url;
    link.download = `revenue-nontech-${this.revenueBucket}.xlsx`;
    link.click();
    URL.revokeObjectURL(url);
  }

  async reconcileChartWithListAndCsv(): Promise<void> {
    this.compareMessage = '';
    this.compareErrorMessage = '';
    if (this.revenueMode !== 'total') {
      this.compareErrorMessage = 'Đối chiếu chart/list/csv hiện chỉ áp dụng cho mode Tổng doanh thu.';
      return;
    }

    try {
      const sharedParams = this.buildReconcileListParams();
      const [listSummary, csvSummary, chartSummary] = await Promise.all([
        this.fetchAllCompletedBillsSummary(sharedParams),
        this.fetchCsvSummary(sharedParams),
        this.fetchChartSummary(),
      ]);

      if (listSummary.totalPayable !== csvSummary.totalPayable) {
        this.compareErrorMessage = `Mismatch list vs csv: ${listSummary.totalPayable} != ${csvSummary.totalPayable}`;
        return;
      }
      if (chartSummary.totalRevenue !== listSummary.totalPayable) {
        this.compareErrorMessage = `Mismatch chart vs list: ${chartSummary.totalRevenue} != ${listSummary.totalPayable}`;
        return;
      }

      this.compareMessage =
        `OK: chart = list = csv = ${chartSummary.totalRevenue.toLocaleString('vi-VN')} VND` +
        ` (count list=${listSummary.totalCount}, csv=${csvSummary.rowCount}, points=${chartSummary.pointCount}).`;
    } catch (error) {
      this.compareErrorMessage = this.resolveApiError(error, 'Không thể đối chiếu chart/list/csv với bộ lọc hiện tại.');
    }
  }

  onChartMouseMove(event: MouseEvent): void {
    if (this.chartPlotPoints.length === 0) {
      this.hoverPoint = null;
      return;
    }

    const canvas = this.revenueChartCanvas?.nativeElement;
    if (!canvas) {
      return;
    }

    const rect = canvas.getBoundingClientRect();
    const scaleX = rect.width > 0 ? canvas.width / rect.width : 1;
    const scaleY = rect.height > 0 ? canvas.height / rect.height : 1;
    const x = (event.clientX - rect.left) * scaleX;
    const y = (event.clientY - rect.top) * scaleY;
    const nearest = this.chartPlotPoints.reduce((prev, curr) => {
      const prevDist = Math.hypot(prev.x - x, prev.y - y);
      const currDist = Math.hypot(curr.x - x, curr.y - y);
      return currDist < prevDist ? curr : prev;
    });

    const distance = Math.hypot(nearest.x - x, nearest.y - y);
    this.hoverPoint = distance <= 24 ? nearest : null;
  }

  onChartMouseLeave(): void {
    this.hoverPoint = null;
  }

  onChartWheel(event: WheelEvent): void {
    if (!event.ctrlKey || this.chartDataFull.length === 0) {
      return;
    }
    event.preventDefault();
    const canvas = this.revenueChartCanvas?.nativeElement;
    if (!canvas) {
      return;
    }

    const span = this.chartViewEnd - this.chartViewStart;
    if (span <= 0) {
      return;
    }

    const maxSpan = this.chartDataFull.length;
    const minSpan = Math.max(1, Math.min(this.chartZoomMinPoints, maxSpan));
    const zoomIn = event.deltaY < 0;
    if (!zoomIn && span >= maxSpan) {
      return;
    }

    let newSpan = zoomIn
      ? Math.max(minSpan, Math.floor(span * 0.82))
      : Math.min(maxSpan, Math.ceil(span / 0.82));
    newSpan = Math.min(maxSpan, Math.max(minSpan, newSpan));
    if (newSpan === span) {
      return;
    }

    const rect = canvas.getBoundingClientRect();
    const scaleX = rect.width > 0 ? canvas.width / rect.width : 1;
    const mouseX = (event.clientX - rect.left) * scaleX;
    const plotLeft = this.chartLayout.left;
    const plotRight = canvas.width - this.chartLayout.rightPad;
    const plotW = Math.max(1, plotRight - plotLeft);
    const t = Math.min(1, Math.max(0, (mouseX - plotLeft) / plotW));
    const anchorIdx = span <= 1 ? this.chartViewStart : this.chartViewStart + t * (span - 1);

    let newStart = Math.round(anchorIdx - t * (newSpan - 1));
    let newEnd = newStart + newSpan;
    if (newStart < 0) {
      newEnd -= newStart;
      newStart = 0;
    }
    if (newEnd > maxSpan) {
      newStart -= newEnd - maxSpan;
      newEnd = maxSpan;
    }
    newStart = Math.max(0, newStart);
    newEnd = Math.min(maxSpan, newEnd);
    if (newEnd - newStart < minSpan) {
      newStart = Math.max(0, newEnd - minSpan);
    }

    this.chartViewStart = newStart;
    this.chartViewEnd = newEnd;
    this.applyChartViewSlice();
    this.renderRevenueChart();
  }

  private async loadFiltersAndData(): Promise<void> {
    this.loading = true;
    this.errorMessage = '';
    try {
      const [buyers, products] = await Promise.all([
        this.requestWithTimeout(this.http.get<Buyer[]>(`${this.apiBaseUrl}/api/buyers`)),
        this.requestWithTimeout(this.http.get<Product[]>(`${this.apiBaseUrl}/api/products`)),
      ]);
      this.buyers = buyers;
      this.products = products;
      await Promise.all([this.loadBills(), this.loadRevenueSeries()]);
    } catch (error) {
      this.errorMessage = this.resolveApiError(error, 'Không tải được dữ liệu filter hoặc danh sách bill.');
      this.chartErrorMessage = this.resolveApiError(error, 'Không tải được dữ liệu biểu đồ doanh thu.');
    } finally {
      this.loading = false;
    }
  }

  private async loadBills(): Promise<void> {
    this.errorMessage = '';
    try {
      const response = await this.requestWithTimeout(
        this.http.get<BillListResponse>(`${this.apiBaseUrl}/api/bills`, {
          params: this.buildParams(),
        }));
      this.bills = response.items;
      this.currentPage = response.page;
      this.totalCount = response.totalCount;
      this.totalPages = response.totalPages;
    } catch (error) {
      this.errorMessage = this.resolveApiError(error, 'Không tải được danh sách bill theo filter hiện tại.');
      this.bills = [];
      this.totalCount = 0;
      this.totalPages = 0;
    }
  }

  private async loadRevenueSeries(): Promise<void> {
    if ((this.revenueMode === 'byBuyer' || this.revenueMode === 'byBuyerAndProduct') && !this.chartBuyerId) {
      this.revenuePoints = [];
      this.chartDataFull = [];
      this.chartViewStart = 0;
      this.chartViewEnd = 0;
      this.chartEffectiveRange = null;
      this.chartErrorMessage = 'Chọn buyer để xem biểu đồ theo buyer.';
      this.renderRevenueChart();
      return;
    }
    if ((this.revenueMode === 'byProduct' || this.revenueMode === 'byBuyerAndProduct') && !this.chartProductId) {
      this.revenuePoints = [];
      this.chartDataFull = [];
      this.chartViewStart = 0;
      this.chartViewEnd = 0;
      this.chartEffectiveRange = null;
      this.chartErrorMessage = 'Chọn product để xem biểu đồ theo product.';
      this.renderRevenueChart();
      return;
    }

    this.chartLoading = true;
    this.chartErrorMessage = '';
    try {
      const response = await this.fetchRevenueSeriesWithOptionalDenseRange();
      this.chartDataFull = response.points;
      this.resetChartViewToFullSeries();
      this.renderRevenueChart();
    } catch (error) {
      this.revenuePoints = [];
      this.chartDataFull = [];
      this.chartViewStart = 0;
      this.chartViewEnd = 0;
      this.chartEffectiveRange = null;
      this.chartErrorMessage = this.resolveApiError(error, 'Không tải được dữ liệu biểu đồ doanh thu.');
      this.renderRevenueChart();
    } finally {
      this.chartLoading = false;
    }
  }

  private buildParams(): HttpParams {
    let params = new HttpParams().set('payableMaxVnd', this.payableMaxVnd);

    if (this.selectedBuyerId) {
      params = params.set('buyerId', this.selectedBuyerId);
    }
    if (this.selectedStatus) {
      params = params.set('status', this.selectedStatus);
    }
    if (this.fromDate) {
      params = params.set('from', this.fromDate);
    }
    if (this.toDate) {
      params = params.set('to', this.toDate);
    }
    for (const productId of this.selectedProductIds) {
      params = params.append('productIds', productId);
    }
    params = params.set('page', this.currentPage);
    params = params.set('pageSize', this.pageSize);

    return params;
  }

  private buildReportParams(): HttpParams {
    let params = new HttpParams()
      .set('mode', this.revenueMode)
      .set('bucket', this.revenueBucket)
      .set('payableMaxVnd', this.payableMaxVnd);

    const from = this.chartFromDate || this.chartEffectiveRange?.from;
    const to = this.chartToDate || this.chartEffectiveRange?.to;
    if (from) {
      params = params.set('from', from);
    }
    if (to) {
      params = params.set('to', to);
    }
    if (this.revenueMode === 'byBuyer' || this.revenueMode === 'byBuyerAndProduct') {
      if (this.chartBuyerId) {
        params = params.set('buyerId', this.chartBuyerId);
      }
    }
    if (this.revenueMode === 'byProduct' || this.revenueMode === 'byBuyerAndProduct') {
      if (this.chartProductId) {
        params = params.set('productId', this.chartProductId);
      }
    }

    return params;
  }

  private buildNontechExportParams(): HttpParams {
    let params = new HttpParams()
      .set('bucket', this.revenueBucket)
      .set('payableMaxVnd', this.payableMaxVnd);

    const from = this.chartFromDate || this.chartEffectiveRange?.from;
    const to = this.chartToDate || this.chartEffectiveRange?.to;
    if (from) {
      params = params.set('from', from);
    }
    if (to) {
      params = params.set('to', to);
    }
    if (this.chartBuyerId) {
      params = params.set('buyerId', this.chartBuyerId);
    }
    if (this.chartProductId) {
      params = params.set('productId', this.chartProductId);
    }
    return params;
  }

  private buildReconcileListParams(): HttpParams {
    let params = new HttpParams()
      .set('status', 'completed')
      .set('timeField', 'completed')
      .set('payableMaxVnd', this.payableMaxVnd);

    if ((this.revenueMode === 'byBuyer' || this.revenueMode === 'byBuyerAndProduct') && this.chartBuyerId) {
      params = params.set('buyerId', this.chartBuyerId);
    }
    const from = this.chartFromDate || this.chartEffectiveRange?.from;
    const to = this.chartToDate || this.chartEffectiveRange?.to;
    if (from) {
      params = params.set('from', from);
    }
    if (to) {
      params = params.set('to', to);
    }
    if ((this.revenueMode === 'byProduct' || this.revenueMode === 'byBuyerAndProduct') && this.chartProductId) {
      params = params.append('productIds', this.chartProductId);
    }
    return params;
  }

  /**
   * Không chọn from/to: 10 kỳ gần nhất (ngày/tuần/tháng) theo lịch VN, dense từ API.
   * Một trong hai biên có giá trị: một request thưa như backend.
   * Cả hai biên: dense theo user.
   */
  private async fetchRevenueSeriesWithOptionalDenseRange(): Promise<RevenueSeriesResponse> {
    const baseParams = this.buildReportParamsWithoutEffectiveDates();

    if (this.chartFromDate || this.chartToDate) {
      this.chartEffectiveRange =
        this.chartFromDate && this.chartToDate
          ? { from: this.chartFromDate, to: this.chartToDate }
          : null;
      return this.requestWithTimeout(
        this.http.get<RevenueSeriesResponse>(`${this.apiBaseUrl}/api/reports/revenue-series`, { params: baseParams }),
      );
    }

    const { fromStr, toStr } = this.computeDefaultVietnamChartRange(this.revenueBucket);
    this.chartEffectiveRange = { from: fromStr, to: toStr };
    const denseParams = baseParams.set('from', fromStr).set('to', toStr);
    return this.requestWithTimeout(
      this.http.get<RevenueSeriesResponse>(`${this.apiBaseUrl}/api/reports/revenue-series`, { params: denseParams }),
    );
  }

  private computeDefaultVietnamChartRange(bucket: RevenueBucket): { fromStr: string; toStr: string } {
    const n = this.chartDefaultPeriodCount;
    const todayVn = this.getVietnamYmd();
    if (bucket === 'day') {
      return {
        fromStr: this.addCalendarDaysYmd(todayVn, -(n - 1)),
        toStr: todayVn,
      };
    }
    if (bucket === 'week') {
      const mondayThisWeek = this.mondayOfWeekContainingYmd(todayVn);
      return {
        fromStr: this.addCalendarDaysYmd(mondayThisWeek, -(n - 1) * 7),
        toStr: todayVn,
      };
    }
    const { fromStr, toStr } = this.defaultMonthRangeVn(todayVn, n);
    return { fromStr, toStr };
  }

  private getVietnamYmd(d = new Date()): string {
    return new Intl.DateTimeFormat('en-CA', {
      timeZone: 'Asia/Ho_Chi_Minh',
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
    }).format(d);
  }

  private addCalendarDaysYmd(ymd: string, delta: number): string {
    const [y, m, d] = ymd.split('-').map(Number);
    const utc = Date.UTC(y, m - 1, d) + delta * 86400000;
    const dt = new Date(utc);
    return `${dt.getUTCFullYear()}-${String(dt.getUTCMonth() + 1).padStart(2, '0')}-${String(dt.getUTCDate()).padStart(2, '0')}`;
  }

  private mondayOfWeekContainingYmd(ymd: string): string {
    const [y, m, d] = ymd.split('-').map(Number);
    const dow = new Date(Date.UTC(y, m - 1, d)).getUTCDay();
    const daysFromMonday = (dow + 6) % 7;
    return this.addCalendarDaysYmd(ymd, -daysFromMonday);
  }

  private defaultMonthRangeVn(todayYmd: string, periodCount: number): { fromStr: string; toStr: string } {
    const [y, m] = todayYmd.split('-').map(Number);
    const M0 = m - 1;
    const lastDay = new Date(Date.UTC(y, M0 + 1, 0)).getUTCDate();
    const toStr = `${y}-${String(m).padStart(2, '0')}-${String(lastDay).padStart(2, '0')}`;

    let Y = y;
    let Mo = m - (periodCount - 1);
    while (Mo <= 0) {
      Mo += 12;
      Y -= 1;
    }
    const fromStr = `${Y}-${String(Mo).padStart(2, '0')}-01`;
    return { fromStr, toStr };
  }

  private resetChartViewToFullSeries(): void {
    this.chartViewStart = 0;
    this.chartViewEnd = this.chartDataFull.length;
    this.applyChartViewSlice();
  }

  private applyChartViewSlice(): void {
    this.revenuePoints = this.chartDataFull.slice(this.chartViewStart, this.chartViewEnd);
  }

  /** Params chart giống buildReportParams nhưng không dùng chartEffectiveRange (tránh vòng lặp khi suy dense). */
  private buildReportParamsWithoutEffectiveDates(): HttpParams {
    let params = new HttpParams()
      .set('mode', this.revenueMode)
      .set('bucket', this.revenueBucket)
      .set('payableMaxVnd', this.payableMaxVnd);

    if (this.chartFromDate) {
      params = params.set('from', this.chartFromDate);
    }
    if (this.chartToDate) {
      params = params.set('to', this.chartToDate);
    }
    if (this.revenueMode === 'byBuyer' || this.revenueMode === 'byBuyerAndProduct') {
      if (this.chartBuyerId) {
        params = params.set('buyerId', this.chartBuyerId);
      }
    }
    if (this.revenueMode === 'byProduct' || this.revenueMode === 'byBuyerAndProduct') {
      if (this.chartProductId) {
        params = params.set('productId', this.chartProductId);
      }
    }

    return params;
  }

  private async fetchAllCompletedBillsSummary(baseParams: HttpParams): Promise<{ totalPayable: number; totalCount: number }> {
    let totalPayable = 0;
    let totalCount = 0;
    let page = 1;
    let totalPages = 1;

    while (page <= totalPages) {
      const params = baseParams.set('page', page).set('pageSize', 20);
      const response = await this.requestWithTimeout(this.http.get<BillListResponse>(`${this.apiBaseUrl}/api/bills`, { params }));
      totalPages = response.totalPages || 1;
      totalCount = response.totalCount;
      totalPayable += response.items.reduce((sum, item) => sum + item.payable, 0);
      page += 1;
    }

    return { totalPayable, totalCount };
  }

  private async fetchCsvSummary(baseParams: HttpParams): Promise<{ totalPayable: number; rowCount: number }> {
    const csvBlob = await this.requestWithTimeout(
      this.http.get(`${this.apiBaseUrl}/api/bills/export.csv`, {
        params: baseParams,
        responseType: 'blob',
      }),
    );
    const csvText = await csvBlob.text();
    const lines = csvText
      .split(/\r?\n/)
      .map((x) => x.trim())
      .filter((x) => x.length > 0);
    if (lines.length <= 1) {
      return { totalPayable: 0, rowCount: 0 };
    }

    const header = lines[0].split(',');
    const payableIndex = header.indexOf('Payable');
    if (payableIndex < 0) {
      throw new Error('CSV missing Payable column');
    }

    let totalPayable = 0;
    for (const line of lines.slice(1)) {
      const cells = line.split(',');
      totalPayable += Number(cells[payableIndex] ?? 0);
    }
    return { totalPayable, rowCount: lines.length - 1 };
  }

  private async fetchChartSummary(): Promise<{ totalRevenue: number; pointCount: number }> {
    const response = await this.requestWithTimeout(
      this.http.get<RevenueSeriesResponse>(`${this.apiBaseUrl}/api/reports/revenue-series`, {
        params: this.buildReportParams(),
      }),
    );
    const totalRevenue = response.points.reduce((sum, point) => sum + point.revenueVnd, 0);
    return { totalRevenue, pointCount: response.points.length };
  }

  private renderRevenueChart(): void {
    const canvas = this.revenueChartCanvas?.nativeElement;
    if (!canvas) {
      return;
    }

    const ctx = canvas.getContext('2d');
    if (!ctx) {
      return;
    }

    const width = canvas.width;
    const height = canvas.height;
    const left = this.chartLayout.left;
    const right = width - this.chartLayout.rightPad;
    const top = this.chartLayout.top;
    const bottom = height - this.chartLayout.bottomPad;

    const theme = this.getChartTheme();
    const colorAxis = theme.axis;
    const colorGrid = theme.grid;
    const colorAxisText = theme.axisText;
    const colorLabelText = theme.labelText;
    const colorPrimary = theme.line;
    const colorPrimaryFillTop = theme.areaTop;
    const colorPrimaryFillBottom = theme.areaBottom;

    ctx.clearRect(0, 0, width, height);
    ctx.fillStyle = '#ffffff';
    ctx.fillRect(0, 0, width, height);
    ctx.strokeStyle = colorAxis;
    ctx.lineWidth = 1;
    ctx.beginPath();
    ctx.moveTo(left, top);
    ctx.lineTo(left, bottom);
    ctx.lineTo(right, bottom);
    ctx.stroke();

    if (this.revenuePoints.length === 0) {
      this.chartPlotPoints = [];
      ctx.fillStyle = colorAxisText;
      ctx.font = '13px Inter, system-ui, sans-serif';
      ctx.fillText('Không có dữ liệu chart theo bộ lọc hiện tại.', left + 8, (top + bottom) / 2);
      return;
    }

    const values = this.revenuePoints.map((x) => x.revenueVnd);
    const minY = 0;
    const maxY = Math.max(...values, 1);
    const xStep = this.revenuePoints.length > 1 ? (right - left) / (this.revenuePoints.length - 1) : 0;

    const tickCount = 4;
    ctx.font = '11px Inter, system-ui, sans-serif';
    for (let i = 0; i <= tickCount; i += 1) {
      const value = Math.round((maxY * i) / tickCount);
      const y = bottom - ((bottom - top) * i) / tickCount;
      ctx.fillStyle = colorAxisText;
      ctx.fillText(this.formatCurrencyShort(value), 8, y + 3);
      ctx.strokeStyle = colorGrid;
      ctx.beginPath();
      ctx.moveTo(left, y);
      ctx.lineTo(right, y);
      ctx.stroke();
    }

    if (this.revenuePoints.length >= 2) {
      const gradient = ctx.createLinearGradient(0, top, 0, bottom);
      gradient.addColorStop(0, colorPrimaryFillTop);
      gradient.addColorStop(1, colorPrimaryFillBottom);
      ctx.fillStyle = gradient;
      ctx.beginPath();
      this.revenuePoints.forEach((point, index) => {
        const x = left + xStep * index;
        const ratio = (point.revenueVnd - minY) / (maxY - minY || 1);
        const y = bottom - ratio * (bottom - top);
        if (index === 0) {
          ctx.moveTo(x, y);
        } else {
          ctx.lineTo(x, y);
        }
      });
      ctx.lineTo(left + xStep * (this.revenuePoints.length - 1), bottom);
      ctx.lineTo(left, bottom);
      ctx.closePath();
      ctx.fill();
    }

    ctx.strokeStyle = colorPrimary;
    ctx.lineWidth = 2;
    ctx.lineJoin = 'round';
    ctx.lineCap = 'round';
    ctx.beginPath();
    this.revenuePoints.forEach((point, index) => {
      const x = left + xStep * index;
      const ratio = (point.revenueVnd - minY) / (maxY - minY || 1);
      const y = bottom - ratio * (bottom - top);
      if (index === 0) {
        ctx.moveTo(x, y);
      } else {
        ctx.lineTo(x, y);
      }
    });
    ctx.stroke();

    this.chartPlotPoints = [];
    this.revenuePoints.forEach((point, index) => {
      const x = left + xStep * index;
      const ratio = (point.revenueVnd - minY) / (maxY - minY || 1);
      const y = bottom - ratio * (bottom - top);
      this.chartPlotPoints.push({
        x,
        y,
        label: this.formatBucketLabel(point.bucketStartLocal),
        value: point.revenueVnd,
      });
      ctx.beginPath();
      ctx.fillStyle = '#ffffff';
      ctx.arc(x, y, 3.5, 0, Math.PI * 2);
      ctx.fill();
      ctx.beginPath();
      ctx.fillStyle = colorPrimary;
      ctx.arc(x, y, 2.5, 0, Math.PI * 2);
      ctx.fill();

      if (
        index === 0 ||
        index === this.revenuePoints.length - 1 ||
        (this.revenuePoints.length <= 8 && index % 2 === 0)
      ) {
        const label = this.formatBucketLabel(point.bucketStartLocal);
        ctx.fillStyle = colorLabelText;
        ctx.font = '11px Inter, system-ui, sans-serif';
        ctx.fillText(label, x - 20, bottom + 18);
      }
    });
  }

  /**
   * CSS custom properties don't change after first paint in this app, so cache them
   * to avoid getComputedStyle on every chart redraw (notably during wheel zoom).
   */
  private getChartTheme(): ChartTheme {
    if (this.chartTheme) {
      return this.chartTheme;
    }
    const cssVars = getComputedStyle(document.documentElement);
    const read = (name: string, fallback: string) => cssVars.getPropertyValue(name).trim() || fallback;
    this.chartTheme = {
      axis: read('--chart-axis', '#cbd5e1'),
      grid: read('--chart-grid', '#eef1f6'),
      axisText: read('--chart-axis-text', '#64748b'),
      labelText: read('--chart-label-text', '#334155'),
      line: read('--chart-line', '#3b82f6'),
      areaTop: read('--chart-area-top', 'rgba(59, 130, 246, 0.18)'),
      areaBottom: read('--chart-area-bottom', 'rgba(59, 130, 246, 0)'),
    };
    return this.chartTheme;
  }

  private formatBucketLabel(value: string): string {
    const date = new Date(value);
    if (Number.isNaN(date.getTime())) {
      return value.slice(0, 10);
    }
    const day = `${date.getDate()}`.padStart(2, '0');
    const month = `${date.getMonth() + 1}`.padStart(2, '0');
    const year = date.getFullYear();
    if (this.revenueBucket === 'month') {
      return `${month}/${year}`;
    }
    if (this.revenueBucket === 'week') {
      return `W ${day}/${month}`;
    }
    return `${day}/${month}`;
  }

  private formatCurrencyShort(value: number): string {
    if (value >= 1_000_000_000) {
      return `${(value / 1_000_000_000).toFixed(1)}B`;
    }
    if (value >= 1_000_000) {
      return `${(value / 1_000_000).toFixed(1)}M`;
    }
    if (value >= 1_000) {
      return `${(value / 1_000).toFixed(1)}K`;
    }
    return `${value}`;
  }

  private requestWithTimeout<T>(request$: Observable<T>): Promise<T> {
    return firstValueFrom(request$.pipe(timeout(this.apiTimeoutMs)));
  }

  private resolveApiError(error: any, fallbackMessage: string): string {
    if (error?.name === 'TimeoutError') {
      return 'Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.';
    }
    return error?.error?.message ?? fallbackMessage;
  }
}

type Buyer = { id: number; name: string; vipPoint: number };
type Product = { id: number; name: string; unitPriceVnd: number };
type BillListItem = {
  billId: number;
  buyerId: number;
  status: string;
  pendingAtUtc: string;
  completedAtUtc?: string | null;
  subtotal: number;
  voucherThuongAmount: number;
  baseBeforeVip: number;
  vipPointUsed: number;
  vipDiscountVnd: number;
  payable: number;
  vipPointEarned: number;
};

type BillListResponse = {
  items: BillListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

type RevenueMode = 'total' | 'byBuyer' | 'byProduct' | 'byBuyerAndProduct';
type RevenueBucket = 'day' | 'week' | 'month';
type RevenuePoint = {
  bucketStartLocal: string;
  revenueVnd: number;
};
type RevenueSeriesResponse = {
  mode: string;
  bucket: string;
  points: RevenuePoint[];
};

type ChartPlotPoint = {
  x: number;
  y: number;
  label: string;
  value: number;
};

type ChartTheme = {
  axis: string;
  grid: string;
  axisText: string;
  labelText: string;
  line: string;
  areaTop: string;
  areaBottom: string;
};
