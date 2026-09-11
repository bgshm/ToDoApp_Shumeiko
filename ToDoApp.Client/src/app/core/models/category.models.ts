export interface CategoryDto {
  id: string;
  name: string;
  color: string;
  taskCount: number;
}

export interface SaveCategoryRequest {
  name: string;
  color: string;
}
