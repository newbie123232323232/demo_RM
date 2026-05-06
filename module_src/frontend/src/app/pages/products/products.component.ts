import { CommonModule } from '@angular/common';
import { HttpClient, HttpErrorResponse, HttpParams } from '@angular/common/http';
import {
  Component,
  ElementRef,
  OnDestroy,
  OnInit,
  ViewChild,
  inject,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Observable, Subject, debounceTime, firstValueFrom, takeUntil, timeout } from 'rxjs';
import { environment } from '../../../environments/environment';

type DialogKind = 'form' | 'delete' | null;
type FormMode = 'create' | 'edit';

interface ProductDto {
  id: number;
  name: string;
  unitPriceVnd: number;
}

interface PagedResponse {
  items: ProductDto[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

interface ApiErrorBody {
  code: string;
  message: string;
}

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

  @ViewChild('formDialog') private formDialog?: ElementRef<HTMLDivElement>;
  @ViewChild('deleteDialog') private deleteDialog?: ElementRef<HTMLDivElement>;
  @ViewChild('nameInput') private nameInput?: ElementRef<HTMLInputElement>;
  @ViewChild('deleteCancelBtn') private deleteCancelBtn?: ElementRef<HTMLButtonElement>;

  products: ProductDto[] = [];
  totalCount = 0;
  totalPages = 0;
  currentPage = 1;
  pageSize: 5 | 10 | 15 | 20 = 10;
  searchTerm = '';
  priceMin: number | null = null;
  priceMax: number | null = null;

  loading = false;
  errorMessage = '';

  openDialog: DialogKind = null;
  formMode: FormMode = 'create';
  formId: number | null = null;
  formName = '';
  formUnitPriceVnd: number | null = null;
  formError = '';
  formNameError = '';
  formPriceError = '';
  formSubmitting = false;

  deleteTarget: ProductDto | null = null;
  deleteError = '';
  deleteSubmitting = false;

  private dialogOpenerEl: HTMLElement | null = null;
  private prevBodyOverflow: string | null = null;
  private readonly searchDebounce$ = new Subject<void>();
  private readonly priceFilterDebounce$ = new Subject<void>();
  private readonly destroy$ = new Subject<void>();

  ngOnInit(): void {
    this.searchDebounce$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        void this.loadProducts();
      });
    this.priceFilterDebounce$
      .pipe(debounceTime(300), takeUntil(this.destroy$))
      .subscribe(() => {
        this.currentPage = 1;
        void this.loadProducts();
      });
    void this.loadProducts();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
    // Restore body overflow if a dialog was open. We deliberately do NOT focus the opener
    // because the component is being torn down (e.g. router navigation) and the opener
    // element may already be detached.
    if (this.openDialog !== null) {
      document.body.style.overflow = this.prevBodyOverflow ?? '';
      this.prevBodyOverflow = null;
      this.dialogOpenerEl = null;
    }
  }

  get hasActiveFilters(): boolean {
    return this.searchTerm.trim().length > 0 || this.priceMin !== null || this.priceMax !== null;
  }

  async loadProducts(): Promise<void> {
    this.loading = true;
    this.errorMessage = '';
    try {
      let params = new HttpParams()
        .set('page', String(this.currentPage))
        .set('pageSize', String(this.pageSize));
      const trimmed = this.searchTerm.trim();
      if (trimmed.length > 0) {
        params = params.set('q', trimmed);
      }
      if (this.priceMin !== null && !Number.isNaN(this.priceMin)) {
        params = params.set('priceMin', String(this.priceMin));
      }
      if (this.priceMax !== null && !Number.isNaN(this.priceMax)) {
        params = params.set('priceMax', String(this.priceMax));
      }

      const resp = await this.requestWithTimeout(
        this.http.get<PagedResponse>(`${this.apiBaseUrl}/api/products`, { params }),
      );
      this.products = resp.items;
      this.totalCount = resp.totalCount;
      this.totalPages = resp.totalPages;
      this.currentPage = resp.page;
      // Backend clamps pageSize at 100; keep UI select in sync only if value is one of the allowed presets.
      if (resp.pageSize === 5 || resp.pageSize === 10 || resp.pageSize === 15 || resp.pageSize === 20) {
        this.pageSize = resp.pageSize;
      }
    } catch (err) {
      this.errorMessage = this.errorToMessage(err, 'Không tải được danh sách sản phẩm.');
      this.products = [];
      this.totalCount = 0;
      this.totalPages = 0;
    } finally {
      this.loading = false;
    }
  }

  onSearchChanged(value: string): void {
    this.searchTerm = value;
    this.searchDebounce$.next();
  }

  onPriceFilterChanged(): void {
    this.priceFilterDebounce$.next();
  }

  async onPageSizeChanged(): Promise<void> {
    this.currentPage = 1;
    await this.loadProducts();
  }

  async previousPage(): Promise<void> {
    if (this.currentPage <= 1) return;
    this.currentPage -= 1;
    await this.loadProducts();
  }

  async nextPage(): Promise<void> {
    if (this.currentPage >= this.totalPages) return;
    this.currentPage += 1;
    await this.loadProducts();
  }

  async resetFilters(): Promise<void> {
    this.searchTerm = '';
    this.priceMin = null;
    this.priceMax = null;
    this.currentPage = 1;
    await this.loadProducts();
  }

  openCreateDialog(): void {
    this.formMode = 'create';
    this.formId = null;
    this.formName = '';
    this.formUnitPriceVnd = null;
    this.resetFormErrors();
    this.openDialog = 'form';
    this.lockBodyAndCaptureOpener();
    // Single tick: try focus the first form field; fall back to dialog shell if not yet rendered.
    setTimeout(() => {
      const target = this.nameInput?.nativeElement ?? this.formDialog?.nativeElement;
      target?.focus();
    }, 0);
  }

  openEditDialog(product: ProductDto): void {
    this.formMode = 'edit';
    this.formId = product.id;
    this.formName = product.name;
    this.formUnitPriceVnd = product.unitPriceVnd;
    this.resetFormErrors();
    this.openDialog = 'form';
    this.lockBodyAndCaptureOpener();
    setTimeout(() => {
      const target = this.nameInput?.nativeElement ?? this.formDialog?.nativeElement;
      target?.focus();
    }, 0);
  }

  openDeleteDialog(product: ProductDto): void {
    this.deleteTarget = product;
    this.deleteError = '';
    this.openDialog = 'delete';
    this.lockBodyAndCaptureOpener();
    // Initial focus on Hủy (cancel) for destructive action — reduces risk of accidental Enter/Space confirming delete.
    setTimeout(() => {
      const target = this.deleteCancelBtn?.nativeElement ?? this.deleteDialog?.nativeElement;
      target?.focus();
    }, 0);
  }

  closeDialog(): void {
    if (this.formSubmitting || this.deleteSubmitting) {
      return;
    }
    this.openDialog = null;
    this.deleteTarget = null;
    this.releaseDialogSideEffects();
  }

  validateNameOnBlur(): void {
    const trimmed = this.formName.trim();
    if (trimmed.length === 0) {
      this.formNameError = 'Tên sản phẩm không được để trống.';
    } else if (trimmed.length > 160) {
      this.formNameError = 'Tên sản phẩm tối đa 160 ký tự.';
    } else {
      this.formNameError = '';
    }
  }

  validatePriceOnBlur(): void {
    if (this.formUnitPriceVnd === null || Number.isNaN(this.formUnitPriceVnd) || this.formUnitPriceVnd <= 0) {
      this.formPriceError = 'Đơn giá phải > 0 VND.';
    } else {
      this.formPriceError = '';
    }
  }

  async submitForm(): Promise<void> {
    this.validateNameOnBlur();
    this.validatePriceOnBlur();
    if (this.formNameError.length > 0 || this.formPriceError.length > 0) {
      return;
    }
    this.formSubmitting = true;
    this.formError = '';
    try {
      const payload = {
        name: this.formName.trim(),
        unitPriceVnd: this.formUnitPriceVnd,
      };
      if (this.formMode === 'create') {
        await this.requestWithTimeout(
          this.http.post<ProductDto>(`${this.apiBaseUrl}/api/products`, payload),
        );
      } else if (this.formId !== null) {
        await this.requestWithTimeout(
          this.http.put<ProductDto>(`${this.apiBaseUrl}/api/products/${this.formId}`, payload),
        );
      }
      this.openDialog = null;
      this.releaseDialogSideEffects();
      await this.loadProducts();
    } catch (err) {
      this.formError = this.errorToMessage(err, 'Không lưu được sản phẩm.');
    } finally {
      this.formSubmitting = false;
    }
  }

  async confirmDelete(): Promise<void> {
    if (!this.deleteTarget) return;
    const id = this.deleteTarget.id;
    this.deleteSubmitting = true;
    this.deleteError = '';
    try {
      await this.requestWithTimeout(
        this.http.delete(`${this.apiBaseUrl}/api/products/${id}`),
      );
      this.openDialog = null;
      this.deleteTarget = null;
      this.releaseDialogSideEffects();
      await this.loadProducts();
    } catch (err) {
      const status = err instanceof HttpErrorResponse ? err.status : 0;
      const code = (err instanceof HttpErrorResponse ? err.error?.code : '') ?? '';
      if (status === 404) {
        this.deleteError = 'Sản phẩm không tồn tại hoặc đã bị xoá. Bấm "Hủy" để đóng và làm mới danh sách.';
        await this.loadProducts();
      } else if (status === 409 && code === 'conflict_product_in_use') {
        this.deleteError = 'Sản phẩm đang được dùng trong bill, không thể xoá.';
      } else {
        this.deleteError = this.errorToMessage(err, 'Không xoá được sản phẩm.');
      }
    } finally {
      this.deleteSubmitting = false;
    }
  }

  onDialogKeydown(event: KeyboardEvent, scope: 'form' | 'delete'): void {
    if (event.key !== 'Tab') return;
    const dialog = scope === 'form' ? this.formDialog?.nativeElement : this.deleteDialog?.nativeElement;
    if (!dialog) return;
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
    // Shift+Tab from first focusable or dialog shell → wrap to last.
    if (event.shiftKey && (active === first || active === dialog)) {
      event.preventDefault();
      last.focus();
      return;
    }
    // Tab from last focusable → wrap to first.
    if (!event.shiftKey && active === last) {
      event.preventDefault();
      first.focus();
      return;
    }
    // Tab from dialog shell (e.g. just opened, focus on `tabindex=-1` container) → forward to first focusable.
    if (!event.shiftKey && active === dialog) {
      event.preventDefault();
      first.focus();
    }
  }

  private lockBodyAndCaptureOpener(): void {
    this.dialogOpenerEl = (document.activeElement as HTMLElement | null) ?? null;
    this.prevBodyOverflow = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
  }

  private releaseDialogSideEffects(): void {
    document.body.style.overflow = this.prevBodyOverflow ?? '';
    this.prevBodyOverflow = null;
    const opener = this.dialogOpenerEl;
    this.dialogOpenerEl = null;
    if (opener && typeof opener.focus === 'function') {
      setTimeout(() => opener.focus(), 0);
    }
  }

  private resetFormErrors(): void {
    this.formError = '';
    this.formNameError = '';
    this.formPriceError = '';
  }

  private errorToMessage(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      const body = err.error as ApiErrorBody | undefined;
      if (body?.message) return body.message;
      if (err.status === 0) return 'Không kết nối được tới API. Kiểm tra server.';
      return `${fallback} (HTTP ${err.status}).`;
    }
    if (err instanceof Error && err.name === 'TimeoutError') {
      return 'Request timeout (>10s). Kiểm tra API/DB hoặc kết nối mạng.';
    }
    return fallback;
  }

  private async requestWithTimeout<T>(observable: Observable<T>): Promise<T> {
    return await firstValueFrom(observable.pipe(timeout(this.apiTimeoutMs)));
  }
}
