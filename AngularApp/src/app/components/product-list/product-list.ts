import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { Subscription } from 'rxjs';
import { ProductService } from '../../services/product';
import { Product } from '../../models/product.model';
import { Header } from '../header/header';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

@Component({
  selector: 'app-product-list',
  standalone: true,
  imports: [CommonModule, FormsModule, Header, Sidebar, Footer],
  templateUrl: './product-list.html',
  styleUrl: './product-list.css'
})
export class ProductList implements OnInit, OnDestroy {
  products: Product[] = [];
  filteredProducts: Product[] = [];
  searchTerm: string = '';
  selectedCategory: string = '';
  categories: string[] = [];
  
  // Pagination
  currentPage: number = 1;
  itemsPerPage: number = 10;
  totalItems: number = 0;
  
  // Sorting
  sortField: keyof Product = 'name';
  sortDirection: 'asc' | 'desc' = 'asc';
  
  private subscription: Subscription = new Subscription();

  constructor(
    private productService: ProductService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.loadProducts();
    this.loadCategories();
  }

  ngOnDestroy(): void {
    this.subscription.unsubscribe();
  }

  loadProducts(): void {
    this.subscription.add(
      this.productService.getProducts().subscribe(products => {
        this.products = products;
        this.applyFiltersAndSort();
      })
    );
  }

  loadCategories(): void {
    this.categories = ['All', ...this.productService.getCategories()];
  }

  applyFiltersAndSort(): void {
    let filtered = [...this.products];

    // Apply search filter
    if (this.searchTerm.trim()) {
      filtered = this.productService.searchProducts(this.searchTerm);
    }

    // Apply category filter
    if (this.selectedCategory && this.selectedCategory !== 'All') {
      filtered = filtered.filter(p => p.category === this.selectedCategory);
    }

    // Apply sorting
    filtered.sort((a, b) => {
      const aValue = a[this.sortField];
      const bValue = b[this.sortField];
      
      let comparison = 0;
      if (aValue < bValue) comparison = -1;
      else if (aValue > bValue) comparison = 1;
      
      return this.sortDirection === 'desc' ? -comparison : comparison;
    });

    this.filteredProducts = filtered;
    this.totalItems = filtered.length;
    this.currentPage = 1; // Reset to first page when filtering
  }

  onSearch(): void {
    this.applyFiltersAndSort();
  }

  onCategoryChange(): void {
    this.applyFiltersAndSort();
  }

  sortBy(field: keyof Product): void {
    if (this.sortField === field) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortField = field;
      this.sortDirection = 'asc';
    }
    this.applyFiltersAndSort();
  }

  getSortIcon(field: keyof Product): string {
    if (this.sortField !== field) return '↕️';
    return this.sortDirection === 'asc' ? '↑' : '↓';
  }

  getPaginatedProducts(): Product[] {
    const startIndex = (this.currentPage - 1) * this.itemsPerPage;
    const endIndex = startIndex + this.itemsPerPage;
    return this.filteredProducts.slice(startIndex, endIndex);
  }

  getTotalPages(): number {
    return Math.ceil(this.totalItems / this.itemsPerPage);
  }

  goToPage(page: number): void {
    if (page >= 1 && page <= this.getTotalPages()) {
      this.currentPage = page;
    }
  }

  previousPage(): void {
    if (this.currentPage > 1) {
      this.currentPage--;
    }
  }

  nextPage(): void {
    if (this.currentPage < this.getTotalPages()) {
      this.currentPage++;
    }
  }

  addProduct(): void {
    this.router.navigate(['/products/new']);
  }

  editProduct(product: Product): void {
    this.router.navigate(['/products/edit', product.productId]);
  }

  deleteProduct(product: Product): void {
    if (confirm(`Are you sure you want to delete "${product.name}"?`)) {
      if (this.productService.deleteProduct(product.productId)) {
        // Product deleted successfully, the subscription will update the list
      } else {
        alert('Failed to delete product');
      }
    }
  }

  getStockStatus(quantity: number): string {
    if (quantity === 0) return 'out-of-stock';
    if (quantity < 10) return 'low-stock';
    return 'in-stock';
  }

  getStockStatusText(quantity: number): string {
    if (quantity === 0) return 'Out of Stock';
    if (quantity < 10) return 'Low Stock';
    return 'In Stock';
  }

  formatCurrency(amount: number): string {
    return new Intl.NumberFormat('en-US', {
      style: 'currency',
      currency: 'USD'
    }).format(amount);
  }

  // Pagination helper methods
  getStartIndex(): number {
    return this.totalItems === 0 ? 0 : (this.currentPage - 1) * this.itemsPerPage + 1;
  }

  getEndIndex(): number {
    const endIndex = this.currentPage * this.itemsPerPage;
    return Math.min(endIndex, this.totalItems);
  }

  getVisiblePages(): number[] {
    const totalPages = this.getTotalPages();
    const visiblePages: number[] = [];
    
    if (totalPages <= 7) {
      // Show all pages if 7 or fewer
      for (let i = 1; i <= totalPages; i++) {
        visiblePages.push(i);
      }
    } else {
      // Show smart pagination with ellipsis
      if (this.currentPage <= 4) {
        // Show first 5 pages + last page
        for (let i = 1; i <= 5; i++) {
          visiblePages.push(i);
        }
        if (totalPages > 6) visiblePages.push(-1); // Ellipsis
        visiblePages.push(totalPages);
      } else if (this.currentPage >= totalPages - 3) {
        // Show first page + last 5 pages
        visiblePages.push(1);
        if (totalPages > 6) visiblePages.push(-1); // Ellipsis
        for (let i = totalPages - 4; i <= totalPages; i++) {
          visiblePages.push(i);
        }
      } else {
        // Show first page + current page group + last page
        visiblePages.push(1);
        visiblePages.push(-1); // Ellipsis
        for (let i = this.currentPage - 1; i <= this.currentPage + 1; i++) {
          visiblePages.push(i);
        }
        visiblePages.push(-1); // Ellipsis
        visiblePages.push(totalPages);
      }
    }
    
    return visiblePages;
  }

  onPageSizeChange(): void {
    this.currentPage = 1; // Reset to first page
    this.applyFiltersAndSort();
  }

}
