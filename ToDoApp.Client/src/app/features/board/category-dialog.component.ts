import { ChangeDetectionStrategy, Component, inject, output, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CategoryDto } from '../../core/models/category.models';
import { BoardStore } from './board.store';

const DEFAULT_COLOR = '#4c6ef5';

@Component({
  selector: 'app-category-dialog',
  imports: [FormsModule],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './category-dialog.component.html',
  styleUrls: ['./dialog.css', './category-dialog.component.css']
})
export class CategoryDialogComponent {
  private readonly store = inject(BoardStore);

  protected readonly categories = this.store.categories;

  protected readonly name = signal('');
  protected readonly color = signal(DEFAULT_COLOR);
  protected readonly editingId = signal<string | null>(null);
  protected readonly confirmingDeleteId = signal<string | null>(null);
  protected readonly error = signal<string | null>(null);
  protected readonly busy = signal(false);

  readonly dismiss = output<void>();

  protected submit(): void {
    const name = this.name().trim();

    if (name.length === 0) {
      this.error.set('Give the category a name.');
      return;
    }

    this.busy.set(true);
    this.error.set(null);

    const request = { name, color: this.color() };
    const editingId = this.editingId();

    const done = (error: string | null) => {
      this.busy.set(false);
      this.error.set(error);

      if (error === null) {
        this.resetForm();
      }
    };

    if (editingId === null) {
      this.store.createCategory(request, done);
    } else {
      this.store.updateCategory(editingId, request, done);
    }
  }

  protected edit(category: CategoryDto): void {
    this.editingId.set(category.id);
    this.name.set(category.name);
    this.color.set(category.color);
    this.confirmingDeleteId.set(null);
    this.error.set(null);
  }

  protected requestDelete(category: CategoryDto): void {
    // First click arms, second click deletes.
    if (this.confirmingDeleteId() !== category.id) {
      this.confirmingDeleteId.set(category.id);
      return;
    }

    this.busy.set(true);

    this.store.deleteCategory(category.id, (error) => {
      this.busy.set(false);
      this.error.set(error);
      this.confirmingDeleteId.set(null);

      if (error === null && this.editingId() === category.id) {
        this.resetForm();
      }
    });
  }

  protected resetForm(): void {
    this.editingId.set(null);
    this.name.set('');
    this.color.set(DEFAULT_COLOR);
    this.confirmingDeleteId.set(null);
  }
}
