import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import { Component, ElementRef, OnDestroy, OnInit, ViewChild, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Subject, Subscription, debounceTime } from 'rxjs';
import { firstValueFrom, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-products-page',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './products.component.html',
  styleUrl: './products.component.css',
})
export class ProductsPageComponent implements OnInit, OnDestroy {
  private readonly http = inject(HttpClient);
  private readonly apiBaseUrl = environment.apiBaseUrl;
  private readonly apiTimeoutMs = 10_000;

  private readonly searchDebounce$ = new Subject<void>();
  private readonly priceFilter$ = new Subject<void>();
  private subs = new Subscription();

  @ViewChild('upsertDialog') private upsertDialog?: ElementRef<HTMLElement>;
  @ViewChild('deleteDialog') private deleteDialog?: ElementRef<HTMLElement>;
  @ViewChild('deleteCancelBtn') private deleteCancelBtn?: ElementRef<HTMLButtonElement>;

  items: ProductListItem[] = [];
  loading = false;
  errorMessage = '';
  filterErrorMessage = '';

  searchTerm = '';
  priceMinInput = '';
  priceMaxInput = '';

  currentPage = 1;
  pageSize: 5 | 10 | 15 | 20 = 20;
  totalCount = 0;
  totalPages = 0;

  upsertOpen = false;
  upsertMode: 'create' | 'edit' = 'create';
  upsertId: number | null = null;
  upsertName = '';
  upsertPriceInput = '';
  upsertNameError = '';
  upsertPriceError = '';
  upsertServerError = '';
  upsertBusy = false;

  deleteOpen = false;
  pendingDelete: ProductListItem | null = null;
  deleteServerError = '';
  deleteBusy = false;

  private prevBodyOverflow: string | null = null;
  private modalOpenerEl: HTMLElement | null = null;
  private listRequestSeq = 0;

  ngOnInit(): void {
    this.subs.add(
      this.searchDebounce$.pipe(debounceTime(300)).subscribe(() => {
        this.currentPage = 1;
        void this.loadList();
      }),
    );
    this.subs.add(
      this.priceFilter$.pipe(debounceTime(300)).subscribe(() => {
        this.currentPage = 1;
        void this.loadList();
      }),
    );
    void this.loadList();
  }

  ngOnDestroy(): void {
    this.subs.unsubscribe();
    if (this.upsertOpen || this.deleteOpen) {
      this.releaseModalSideEffects();
    }
  }

  onSearchInput(value: string): void {
    this.searchTerm = value;
    this.searchDebounce$.next();
  }

  onPriceMinInput(value: string): void {
    this.priceMinInput = value;
    this.priceFilter$.next();
  }

  onPriceMaxInput(value: string): void {
    this.priceMaxInput = value;
    this.priceFilter$.next();
  }

  resetFilters(): void {
    this.searchTerm = '';
    this.priceMinInput = '';
    this.priceMaxInput = '';
    this.filterErrorMessage = '';
    this.currentPage = 1;
    void this.loadList();
  }

  async onPageSizeChange(raw: number | string): Promise<void> {
    const n = typeof raw === 'string' ? Number(raw) : raw;
    const size = n === 5 || n === 10 || n === 15 || n === 20 ? n : 20;
    this.pageSize = size;
    this.currentPage = 1;
    await this.loadList();
  }

  async prevPage(): Promise<void> {
    if (this.currentPage <= 1) {
      return;
    }
    this.currentPage -= 1;
    await this.loadList();
  }

  async nextPage(): Promise<void> {
    if (!this.canGoNext) {
      return;
    }
    this.currentPage += 1;
    await this.loadList();
  }

  get canGoNext(): boolean {
    if (this.totalPages <= 0) {
      return false;
    }
    return this.currentPage < this.totalPages;
  }

  onAddClick(): void {
    this.openUpsert('create');
  }

  onEditClick(item: ProductListItem): void {
    this.openUpsert('edit', item);
  }

  onDeleteClick(item: ProductListItem): void {
    if (this.upsertOpen || this.deleteOpen) {
      return;
    }
    this.modalOpenerEl = (document.activeElement as HTMLElement | null) ?? null;
    this.pendingDelete = item;
    this.deleteServerError = '';
    this.deleteOpen = true;
    this.lockBodyScroll();
    setTimeout(() => this.deleteCancelBtn?.nativeElement?.focus(), 0);
  }

  onUpsertBackdropClick(event: MouseEvent): void {
    if (event.target !== event.currentTarget || this.upsertBusy) {
      return;
    }
    this.closeUpsert();
  }

  onDeleteBackdropClick(event: MouseEvent): void {
    if (event.target !== event.currentTarget || this.deleteBusy) {
      return;
    }
    this.closeDelete();
  }

  onUpsertEscape(): void {
    if (!this.upsertBusy) {
      this.closeUpsert();
    }
  }

  onDeleteEscape(): void {
    if (!this.deleteBusy) {
      this.closeDelete();
    }
  }

  onUpsertKeydown(event: KeyboardEvent): void {
    this.trapFocus(event, this.upsertDialog);
  }

  onDeleteKeydown(event: KeyboardEvent): void {
    this.trapFocus(event, this.deleteDialog);
  }

  onUpsertNameInput(): void {
    this.upsertServerError = '';
  }

  onUpsertPriceInput(): void {
    this.upsertServerError = '';
  }

  onUpsertNameBlur(): void {
    this.upsertNameError = this.validateName(this.upsertName);
  }

  onUpsertPriceBlur(): void {
    this.upsertPriceError = this.validatePrice(this.upsertPriceInput);
  }

  closeUpsert(): void {
    if (this.upsertBusy) {
      return;
    }
    this.upsertOpen = false;
    this.clearUpsertForm();
    this.releaseModalSideEffects();
  }

  closeDelete(): void {
    if (this.deleteBusy) {
      return;
    }
    this.deleteOpen = false;
    this.pendingDelete = null;
    this.deleteServerError = '';
    this.releaseModalSideEffects();
  }

  async submitUpsert(): Promise<void> {
    this.upsertNameError = this.validateName(this.upsertName);
    this.upsertPriceError = this.validatePrice(this.upsertPriceInput);
    if (this.upsertNameError || this.upsertPriceError) {
      return;
    }
    const name = this.upsertName.trim();
    const unitPriceVnd = Number(this.upsertPriceInput.trim());
    this.upsertServerError = '';
    this.upsertBusy = true;
    const body = { name, unitPriceVnd };
    try {
      if (this.upsertMode === 'create') {
        await firstValueFrom(
          this.http.post<ProductListItem>(`${this.apiBaseUrl}/api/products`, body).pipe(timeout(this.apiTimeoutMs)),
        );
      } else if (this.upsertId != null) {
        await firstValueFrom(
          this.http
            .put<ProductListItem>(`${this.apiBaseUrl}/api/products/${this.upsertId}`, body)
            .pipe(timeout(this.apiTimeoutMs)),
        );
      }
      this.upsertOpen = false;
      this.clearUpsertForm();
      this.releaseModalSideEffects();
      await this.loadList();
    } catch (e: unknown) {
      this.upsertServerError = this.formatUpsertError(e);
    } finally {
      this.upsertBusy = false;
    }
  }

  async confirmDelete(): Promise<void> {
    if (!this.pendingDelete) {
      return;
    }
    this.deleteServerError = '';
    this.deleteBusy = true;
    const id = this.pendingDelete.id;
    try {
      await firstValueFrom(
        this.http.delete<void>(`${this.apiBaseUrl}/api/products/${id}`).pipe(timeout(this.apiTimeoutMs)),
      );
      const shouldMovePrevPage = this.items.length === 1 && this.currentPage > 1;
      if (shouldMovePrevPage) {
        this.currentPage -= 1;
      }
      this.deleteOpen = false;
      this.pendingDelete = null;
      this.releaseModalSideEffects();
      await this.loadList();
    } catch (e: unknown) {
      this.deleteServerError = this.formatDeleteError(e);
    } finally {
      this.deleteBusy = false;
    }
  }

  get upsertTitle(): string {
    return this.upsertMode === 'create'
      ? 'Thêm sản phẩm mới'
      : `Sửa sản phẩm #${this.upsertId ?? ''}`;
  }

  private openUpsert(mode: 'create' | 'edit', item?: ProductListItem): void {
    if (this.deleteOpen || this.upsertOpen) {
      return;
    }
    this.modalOpenerEl = (document.activeElement as HTMLElement | null) ?? null;
    this.upsertMode = mode;
    this.upsertServerError = '';
    this.upsertNameError = '';
    this.upsertPriceError = '';
    if (mode === 'create') {
      this.upsertId = null;
      this.upsertName = '';
      this.upsertPriceInput = '';
    } else if (item) {
      this.upsertId = item.id;
      this.upsertName = item.name;
      this.upsertPriceInput = String(item.unitPriceVnd);
    }
    this.upsertOpen = true;
    this.lockBodyScroll();
    setTimeout(() => this.upsertDialog?.nativeElement?.focus(), 0);
  }

  private clearUpsertForm(): void {
    this.upsertId = null;
    this.upsertName = '';
    this.upsertPriceInput = '';
    this.upsertNameError = '';
    this.upsertPriceError = '';
    this.upsertServerError = '';
  }

  private lockBodyScroll(): void {
    if (this.prevBodyOverflow !== null) {
      return;
    }
    this.prevBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
  }

  private releaseModalSideEffects(): void {
    document.body.style.overflow = this.prevBodyOverflow ?? '';
    this.prevBodyOverflow = null;
    const opener = this.modalOpenerEl;
    this.modalOpenerEl = null;
    if (opener && typeof opener.focus === 'function') {
      setTimeout(() => opener.focus(), 0);
    }
  }

  private trapFocus(event: KeyboardEvent, dialogRef: ElementRef<HTMLElement> | undefined): void {
    if (event.key !== 'Tab') {
      return;
    }
    const dialog = dialogRef?.nativeElement;
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
    } else if (!event.shiftKey && active === dialog) {
      event.preventDefault();
      first.focus();
    }
  }

  private validateName(raw: string): string {
    const t = raw.trim();
    if (t.length === 0) {
      return 'Tên sản phẩm không được để trống.';
    }
    if (t.length > 160) {
      return 'Tên sản phẩm tối đa 160 ký tự.';
    }
    return '';
  }

  private validatePrice(raw: string): string {
    const t = raw.trim();
    if (t.length === 0) {
      return 'Đơn giá phải lớn hơn 0 VND.';
    }
    const n = Number(t);
    if (!Number.isFinite(n)) {
      return 'Đơn giá không hợp lệ.';
    }
    if (!Number.isInteger(n)) {
      return 'Đơn giá phải là số nguyên dương (VND).';
    }
    if (n <= 0) {
      return 'Đơn giá phải lớn hơn 0 VND.';
    }
    return '';
  }

  private formatUpsertError(e: unknown): string {
    if (e && typeof e === 'object' && 'name' in e && (e as { name?: string }).name === 'TimeoutError') {
      return 'Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.';
    }
    if (e instanceof HttpErrorResponse) {
      const fromBody = this.messageFromApiBody(e.error);
      if (fromBody) {
        return fromBody;
      }
      if (e.status === 404) {
        return 'Sản phẩm không tồn tại hoặc đã bị xoá.';
      }
      if (e.status === 0) {
        return 'Không kết nối được tới API. Kiểm tra server đang chạy.';
      }
    }
    return 'Không lưu được sản phẩm. Thử lại sau.';
  }

  private formatDeleteError(e: unknown): string {
    if (e && typeof e === 'object' && 'name' in e && (e as { name?: string }).name === 'TimeoutError') {
      return 'Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.';
    }
    if (e instanceof HttpErrorResponse) {
      const fromBody = this.messageFromApiBody(e.error);
      if (fromBody) {
        return fromBody;
      }
      if (e.status === 404) {
        return 'Sản phẩm không tồn tại hoặc đã bị xoá.';
      }
      if (e.status === 409) {
        return 'Sản phẩm đang được dùng trong bill, không thể xoá.';
      }
      if (e.status === 0) {
        return 'Không kết nối được tới API. Kiểm tra server đang chạy.';
      }
    }
    return 'Không xoá được sản phẩm. Thử lại sau.';
  }

  private messageFromApiBody(body: unknown): string | null {
    if (body && typeof body === 'object' && 'message' in body) {
      const msg = (body as { message?: unknown }).message;
      if (typeof msg === 'string' && msg.length > 0) {
        return msg;
      }
    }
    return null;
  }

  private async loadList(): Promise<void> {
    const requestSeq = ++this.listRequestSeq;
    this.loading = true;
    this.errorMessage = '';
    this.filterErrorMessage = '';
    try {
      const q = this.searchTerm.trim();
      const priceMin = this.parseOptionalLong(this.priceMinInput);
      const priceMax = this.parseOptionalLong(this.priceMaxInput);
      if (priceMin !== null && priceMax !== null && priceMin > priceMax) {
        this.filterErrorMessage = 'Giá tối thiểu phải nhỏ hơn hoặc bằng giá tối đa.';
        this.loading = false;
        return;
      }

      let params = new HttpParams()
        .set('page', String(this.currentPage))
        .set('pageSize', String(this.pageSize));
      if (q.length > 0) {
        params = params.set('q', q);
      }
      if (priceMin !== null) {
        params = params.set('priceMin', String(priceMin));
      }
      if (priceMax !== null) {
        params = params.set('priceMax', String(priceMax));
      }

      const url = `${this.apiBaseUrl}/api/products`;
      const resp = await firstValueFrom(
        this.http.get<ProductListResponse>(url, { params }).pipe(timeout(this.apiTimeoutMs)),
      );
      if (requestSeq !== this.listRequestSeq) {
        return;
      }
      this.items = resp.items ?? [];
      this.currentPage = resp.page;
      this.pageSize = this.normalizePageSize(resp.pageSize);
      this.totalCount = resp.totalCount;
      this.totalPages = resp.totalPages;
    } catch (e: unknown) {
      if (requestSeq !== this.listRequestSeq) {
        return;
      }
      this.items = [];
      this.totalCount = 0;
      this.totalPages = 0;
      this.errorMessage = this.formatLoadError(e);
    } finally {
      if (requestSeq === this.listRequestSeq) {
        this.loading = false;
      }
    }
  }

  private parseOptionalLong(raw: string): number | null {
    const t = raw.trim();
    if (t.length === 0) {
      return null;
    }
    const n = Number(t);
    if (!Number.isFinite(n) || !Number.isInteger(n)) {
      return null;
    }
    return n;
  }

  private normalizePageSize(n: number): 5 | 10 | 15 | 20 {
    if (n === 5 || n === 10 || n === 15 || n === 20) {
      return n;
    }
    return 20;
  }

  private formatLoadError(e: unknown): string {
    if (e && typeof e === 'object' && 'name' in e && (e as { name?: string }).name === 'TimeoutError') {
      return 'Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.';
    }
    if (e instanceof HttpErrorResponse) {
      const body = e.error;
      if (body && typeof body === 'object' && 'message' in body) {
        const msg = (body as { message?: unknown }).message;
        if (typeof msg === 'string' && msg.length > 0) {
          return msg;
        }
      }
      if (e.status === 0) {
        return 'Không kết nối được tới API. Kiểm tra server đang chạy.';
      }
    }
    return 'Không tải được danh sách sản phẩm. Kiểm tra API đang chạy và thử lại.';
  }
}

interface ProductListItem {
  id: number;
  name: string;
  unitPriceVnd: number;
}

interface ProductListResponse {
  items: ProductListItem[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}
