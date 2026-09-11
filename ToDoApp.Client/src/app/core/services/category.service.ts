import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { API_ROUTES, apiUrl } from '../api.config';
import { CategoryDto, SaveCategoryRequest } from '../models/category.models';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<CategoryDto[]> {
    return this.http.get<CategoryDto[]>(apiUrl(API_ROUTES.categories));
  }

  create(request: SaveCategoryRequest): Observable<CategoryDto> {
    return this.http.post<CategoryDto>(apiUrl(API_ROUTES.categories), request);
  }

  update(id: string, request: SaveCategoryRequest): Observable<CategoryDto> {
    return this.http.put<CategoryDto>(`${apiUrl(API_ROUTES.categories)}/${id}`, request);
  }

  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${apiUrl(API_ROUTES.categories)}/${id}`);
  }
}
