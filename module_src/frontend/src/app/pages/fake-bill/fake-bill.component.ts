import { CommonModule } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-fake-bill-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './fake-bill.component.html',
  styleUrl: './fake-bill.component.css',
})
export class FakeBillPageComponent implements OnInit {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly apiTimeoutMs = 10_000;

  buyers: Buyer[] = [];
  products: Product[] = [];
  draftLines: DraftLine[] = [];

  selectedBuyerId: number | null = null;
  selectedProductId: number | null = null;
  selectedQty = 1;
  voucherType: VoucherType = 'none';
  voucherValue = 0;
  vipPointsUsed = 0;

  loading = false;
  errorMessage = '';
  previewErrorMessage = '';
  previewResult: PreviewResult | null = null;
  private previewDebounceTimer: ReturnType<typeof setTimeout> | null = null;
  currentBill: CurrentBill | null = null;
  pendingBills: PendingBillSummary[] = [];
  actionErrorMessage = '';
  actionInfoMessage = '';

  ngOnInit(): void {
    this.loadInitialData();
  }

  get selectedBuyer(): Buyer | undefined {
    return this.buyers.find((x) => x.id === this.selectedBuyerId);
  }

  get subtotalVnd(): number {
    return this.draftLines.reduce((sum, x) => sum + x.lineTotalVnd, 0);
  }

  onBuyerChanged(nextBuyerId: number | null): void {
    if (nextBuyerId !== this.selectedBuyerId) {
      this.selectedBuyerId = nextBuyerId;
      this.draftLines = [];
      this.voucherType = 'none';
      this.voucherValue = 0;
      this.vipPointsUsed = 0;
      this.previewResult = null;
      this.previewErrorMessage = '';
      this.currentBill = null;
      this.actionErrorMessage = '';
      this.actionInfoMessage = '';
    }
  }

  addDraftLine(): void {
    if (!this.selectedBuyerId) {
      this.errorMessage = 'Vui lòng chọn buyer trước khi thêm sản phẩm.';
      return;
    }

    if (!this.selectedProductId) {
      this.errorMessage = 'Vui lòng chọn product.';
      return;
    }

    if (!Number.isInteger(this.selectedQty) || this.selectedQty <= 0) {
      this.errorMessage = 'Số lượng phải là số nguyên dương.';
      return;
    }

    const product = this.products.find((x) => x.id === this.selectedProductId);
    if (!product) {
      this.errorMessage = 'Product không tồn tại.';
      return;
    }

    const existingLine = this.draftLines.find((x) => x.productId === product.id);
    if (existingLine) {
      existingLine.qty += this.selectedQty;
      existingLine.lineTotalVnd = existingLine.qty * existingLine.unitPriceVnd;
    } else {
      this.draftLines.push({
        productId: product.id,
        productName: product.name,
        unitPriceVnd: product.unitPriceVnd,
        qty: this.selectedQty,
        lineTotalVnd: product.unitPriceVnd * this.selectedQty,
      });
    }

    this.errorMessage = '';
    this.selectedQty = 1;
    this.schedulePreview();
  }

  removeLine(productId: number): void {
    this.draftLines = this.draftLines.filter((x) => x.productId !== productId);
    this.schedulePreview();
  }

  onPricingInputChanged(): void {
    this.schedulePreview();
  }

  async confirmPayment(): Promise<void> {
    if (!this.selectedBuyerId || this.draftLines.length === 0) {
      this.actionErrorMessage = 'Draft phải có buyer và ít nhất 1 dòng trước khi xác nhận.';
      return;
    }

    try {
      const payload = this.buildPricingPayload();
      const response = await this.requestWithTimeout(
        this.http.post<ConfirmResponse>(`${this.apiBaseUrl}/api/bills/confirm`, payload),
      );

      this.currentBill = {
        billId: response.billId,
        status: response.status,
      };
      this.actionErrorMessage = '';
      this.actionInfoMessage = `Đã confirm bill #${response.billId} (Pending).`;
      await this.refreshSelectedBuyer();
      await this.loadPendingBills();
    } catch (error: any) {
      this.actionErrorMessage = this.resolveApiError(error, 'Confirm thất bại. Kiểm tra dữ liệu hoặc backend.');
    }
  }

  async completeBill(): Promise<void> {
    if (!this.currentBill) {
      this.actionErrorMessage = 'Chưa có bill Pending để complete.';
      return;
    }

    try {
      const response = await this.requestWithTimeout(
        this.http.post<CompleteResponse>(
          `${this.apiBaseUrl}/api/bills/${this.currentBill.billId}/complete`,
          {},
        ),
      );

      this.currentBill = {
        billId: response.billId,
        status: response.status,
      };
      this.actionErrorMessage = '';
      this.actionInfoMessage = `Bill #${response.billId} đã Completed.`;
      await this.refreshSelectedBuyer();
      await this.loadPendingBills();
    } catch (error: any) {
      this.actionErrorMessage = this.resolveApiError(error, 'Complete thất bại. Có thể bill đã complete trước đó.');
    }
  }

  async updatePendingBill(): Promise<void> {
    if (!this.currentBill || this.currentBill.status !== 'Pending') {
      this.actionErrorMessage = 'Không có bill Pending để cập nhật.';
      return;
    }

    try {
      const payload = this.buildPricingPayload();
      const response = await this.requestWithTimeout(
        this.http.put<ConfirmResponse>(
          `${this.apiBaseUrl}/api/bills/${this.currentBill.billId}/pending`,
          payload,
        ),
      );
      this.currentBill = { billId: response.billId, status: response.status };
      this.actionErrorMessage = '';
      this.actionInfoMessage = `Đã cập nhật bill Pending #${response.billId}.`;
      await this.refreshSelectedBuyer();
      await this.loadPendingBills();
    } catch (error: any) {
      this.actionErrorMessage = this.resolveApiError(error, 'Cập nhật pending thất bại.');
    }
  }

  async resumePendingBill(billId: number): Promise<void> {
    try {
      const bill = await this.requestWithTimeout(
        this.http.get<BillDetailResponse>(`${this.apiBaseUrl}/api/bills/${billId}`),
      );

      this.selectedBuyerId = bill.buyerId;
      this.voucherType = (bill.voucherType ?? 'none') as VoucherType;
      this.voucherValue = bill.voucherValue ?? 0;
      this.vipPointsUsed = bill.vipPointUsed;
      this.draftLines = bill.lines.map((x) => {
        const product = this.products.find((p) => p.id === x.productId);
        return {
          productId: x.productId,
          productName: product?.name ?? `Product #${x.productId}`,
          unitPriceVnd: x.unitPriceVnd,
          qty: x.qty,
          lineTotalVnd: x.lineTotalVnd,
        };
      });
      this.currentBill = { billId: bill.billId, status: bill.status };
      this.actionErrorMessage = '';
      this.actionInfoMessage = `Đang tiếp tục bill Pending #${bill.billId}.`;
      this.schedulePreview();
    } catch (error) {
      this.actionErrorMessage = this.resolveApiError(error, `Không nạp được bill #${billId}.`);
    }
  }

  private loadInitialData(): void {
    this.loading = true;
    this.errorMessage = '';

    Promise.all([
      this.requestWithTimeout(this.http.get<Buyer[]>(`${this.apiBaseUrl}/api/buyers`)),
      this.requestWithTimeout(this.http.get<Product[]>(`${this.apiBaseUrl}/api/products`)),
    ])
      .then(([buyers, products]) => {
        this.buyers = buyers ?? [];
        this.products = products ?? [];
        void this.loadPendingBills();
      })
      .catch((error) => {
        this.errorMessage = this.resolveApiError(error, 'Không tải được buyers/products từ API. Kiểm tra backend và /health.');
      })
      .finally(() => {
        this.loading = false;
      });
  }

  private schedulePreview(): void {
    this.previewErrorMessage = '';
    if (this.previewDebounceTimer) {
      clearTimeout(this.previewDebounceTimer);
    }

    if (!this.selectedBuyerId || this.draftLines.length === 0) {
      this.previewResult = null;
      return;
    }

    this.previewDebounceTimer = setTimeout(() => {
      void this.fetchPreview();
    }, 300);
  }

  private buildPricingPayload() {
    return {
      buyerId: this.selectedBuyerId,
      lines: this.draftLines.map((x) => ({
        productId: x.productId,
        qty: x.qty,
        unitPriceVnd: x.unitPriceVnd,
      })),
      voucherType: this.voucherType,
      voucherValue: this.voucherValue,
      vipPointsUsed: this.vipPointsUsed,
    };
  }

  private async fetchPreview(): Promise<void> {
    if (!this.selectedBuyerId || this.draftLines.length === 0) {
      return;
    }

    try {
      this.previewResult = await this.requestWithTimeout(
        this.http.post<PreviewResult>(`${this.apiBaseUrl}/api/bills/preview`, this.buildPricingPayload()),
      );
      this.previewErrorMessage = '';
    } catch (error: any) {
      this.previewResult = null;
      this.previewErrorMessage = this.resolveApiError(error, 'Preview lỗi validation hoặc backend chưa sẵn sàng.');
    }
  }

  private async refreshSelectedBuyer(): Promise<void> {
    if (!this.selectedBuyerId) {
      return;
    }

    const updatedBuyer = await this.requestWithTimeout(
      this.http.get<Buyer>(`${this.apiBaseUrl}/api/buyers/${this.selectedBuyerId}`),
    );

    this.buyers = this.buyers.map((x) => (x.id === updatedBuyer.id ? updatedBuyer : x));
  }

  private async loadPendingBills(): Promise<void> {
    try {
      const response = await this.requestWithTimeout(
        this.http.get<BillListResponse>(`${this.apiBaseUrl}/api/bills?status=pending&page=1&pageSize=20`),
      );
      this.pendingBills = response.items;
    } catch (error) {
      this.pendingBills = [];
      this.actionErrorMessage = this.resolveApiError(error, 'Không tải được danh sách bill Pending.');
    }
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

type Buyer = {
  id: number;
  name: string;
  vipPoint: number;
};

type Product = {
  id: number;
  name: string;
  unitPriceVnd: number;
};

type DraftLine = {
  productId: number;
  productName: string;
  unitPriceVnd: number;
  qty: number;
  lineTotalVnd: number;
};

type VoucherType = 'none' | 'percent' | 'vnd';

type PreviewResult = {
  subtotal: number;
  voucherThuongAmount: number;
  baseBeforeVip: number;
  vipDiscount: number;
  payable: number;
  expectedVipPointsEarnedIfCompleted: number;
};

type CurrentBill = {
  billId: number;
  status: string;
};

type ConfirmResponse = {
  billId: number;
  status: string;
};

type CompleteResponse = {
  billId: number;
  status: string;
};

type PendingBillSummary = {
  billId: number;
  buyerId: number;
  status: string;
  payable: number;
  pendingAtUtc: string;
  vipPointUsed: number;
};

type BillListResponse = {
  items: PendingBillSummary[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
};

type BillDetailResponse = {
  billId: number;
  buyerId: number;
  status: string;
  voucherType?: string;
  voucherValue?: number;
  vipPointUsed: number;
  lines: Array<{
    productId: number;
    qty: number;
    unitPriceVnd: number;
    lineTotalVnd: number;
  }>;
};
