import { Component, OnInit, Input, Output, EventEmitter, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule, FormBuilder, FormGroup, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ProductService } from '../../services/product';
import { Product } from '../../models/product.model';
import { Header } from '../header/header';
import { Sidebar } from '../sidebar/sidebar';
import { Footer } from '../footer/footer';

@Component({
  selector: 'app-product-form',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, Header, Sidebar, Footer],
  templateUrl: './product-form.html',
  styleUrl: './product-form.css'
})
export class ProductForm implements OnInit {
  @Input() productId?: number;
  @Output() save = new EventEmitter<void>();
  @Output() cancelled = new EventEmitter<void>();

  private readonly fb = inject(FormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);

  productForm: FormGroup;
  isEditMode = false;
  isLoading = false;
  error = '';
  product?: Product;

  categories = [
    'Electronics',
    'Clothing',
    'Food & Beverages',
    'Books',
    'Home & Garden',
    'Sports & Outdoors',
    'Health & Beauty',
    'Automotive',
    'Toys & Games',
    'Office Supplies'
  ];

  constructor() {
    this.productForm = this.fb.group({
      name: ['', [Validators.required, Validators.minLength(2)]],
      category: ['', Validators.required],
      price: [0, [Validators.required, Validators.min(0.01)]],
      stockQuantity: [0, [Validators.required, Validators.min(0)]]
    });
  }

  ngOnInit(): void {
    // Check if we have a productId input or from route
    if (this.productId) {
      this.loadProduct(this.productId);
    } else {
      const id = this.route.snapshot.params['id'];
      if (id && id !== 'new') {
        this.loadProduct(Number.parseInt(id));
      }
    }
  }

  loadProduct(id: number): void {
    this.isEditMode = true;
    this.isLoading = true;
    
    const product = this.productService.getProductById(id);
    if (product) {
      this.product = product;
      this.productForm.patchValue({
        name: product.name,
        category: product.category,
        price: product.price,
        stockQuantity: product.stockQuantity
      });
    } else {
      this.error = 'Product not found';
    }
    
    this.isLoading = false;
  }

  onSubmit(): void {
    if (this.productForm.valid) {
      this.isLoading = true;
      this.error = '';

      const formValue = this.productForm.value;
      
      try {
        if (this.isEditMode && this.product) {
          // Update existing product
          const updatedProduct = this.productService.updateProduct({
            productId: this.product.productId,
            name: formValue.name,
            category: formValue.category,
            price: formValue.price,
            stockQuantity: formValue.stockQuantity
          });
          
          if (updatedProduct) {
            this.save.emit();
            if (!this.productId) {
              this.router.navigate(['/products']);
            }
          } else {
            this.error = 'Failed to update product';
          }
        } else {
          // Create new product
          this.productService.createProduct({
            name: formValue.name,
            category: formValue.category,
            price: formValue.price,
            stockQuantity: formValue.stockQuantity
          });
          
          this.save.emit();
          if (!this.productId) {
            this.router.navigate(['/products']);
          }
        }
      } catch (error) {
        this.error = 'An error occurred while saving the product';
        console.error('Error saving product:', error);
      } finally {
        this.isLoading = false;
      }
    } else {
      this.markFormGroupTouched();
    }
  }

  onCancel(): void {
    this.cancelled.emit();
    if (!this.productId) {
      this.router.navigate(['/products']);
    }
  }

  private markFormGroupTouched(): void {
    for (const key of Object.keys(this.productForm.controls)) {
      const control = this.productForm.get(key);
      control?.markAsTouched();
    }
  }

  // Getter methods for form validation
  get nameControl() {
    return this.productForm.get('name');
  }

  get categoryControl() {
    return this.productForm.get('category');
  }

  get priceControl() {
    return this.productForm.get('price');
  }

  get stockQuantityControl() {
    return this.productForm.get('stockQuantity');
  }

  isFieldInvalid(fieldName: string): boolean {
    const field = this.productForm.get(fieldName);
    return !!(field && field.invalid && (field.dirty || field.touched));
  }

  getFieldError(fieldName: string): string {
    const field = this.productForm.get(fieldName);
    if (field?.errors && (field.dirty || field.touched)) {
      if (field.errors['required']) {
        return `${this.getFieldLabel(fieldName)} is required`;
      }
      if (field.errors['minlength']) {
        return `${this.getFieldLabel(fieldName)} must be at least ${field.errors['minlength'].requiredLength} characters`;
      }
      if (field.errors['min']) {
        return `${this.getFieldLabel(fieldName)} must be greater than ${field.errors['min'].min}`;
      }
    }
    return '';
  }

  private getFieldLabel(fieldName: string): string {
    const labels: { [key: string]: string } = {
      name: 'Product Name',
      category: 'Category',
      price: 'Price',
      stockQuantity: 'Stock Quantity'
    };
    return labels[fieldName] || fieldName;
  }
}
