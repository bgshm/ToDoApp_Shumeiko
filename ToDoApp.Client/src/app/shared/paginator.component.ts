import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';

/** Compact page control for one board column. */
@Component({
  selector: 'app-paginator',
  changeDetection: ChangeDetectionStrategy.OnPush,
  template: `
    <div class="paginator" [class.paginator--idle]="totalPages() <= 1">
      <span class="paginator__range">{{ rangeLabel() }}</span>

      @if (totalPages() > 1) {
        <span class="paginator__controls">
          <button
            type="button"
            class="paginator__step"
            [disabled]="page() <= 1"
            (click)="pageChange.emit(page() - 1)"
            aria-label="Previous page">
            <i class="bi bi-chevron-left" aria-hidden="true"></i>
          </button>

          <span class="paginator__position">{{ page() }} / {{ totalPages() }}</span>

          <button
            type="button"
            class="paginator__step"
            [disabled]="page() >= totalPages()"
            (click)="pageChange.emit(page() + 1)"
            aria-label="Next page">
            <i class="bi bi-chevron-right" aria-hidden="true"></i>
          </button>
        </span>
      }
    </div>
  `,
  styleUrl: './paginator.component.css'
})
export class PaginatorComponent {
  readonly page = input.required<number>();
  readonly totalPages = input.required<number>();
  readonly totalCount = input.required<number>();
  readonly pageSize = input.required<number>();

  readonly pageChange = output<number>();

  protected readonly rangeLabel = computed(() => {
    const total = this.totalCount();

    if (total === 0) {
      return 'No cards';
    }

    const first = (this.page() - 1) * this.pageSize() + 1;
    const last = Math.min(first + this.pageSize() - 1, total);

    return first === last ? `${first} of ${total}` : `${first}\u2013${last} of ${total}`;
  });
}
