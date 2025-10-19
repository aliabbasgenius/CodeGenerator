import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { Product, CreateProductRequest, UpdateProductRequest } from '../models/product.model';

@Injectable({
  providedIn: 'root'
})
export class ProductService {
  private products: Product[] = [
    { productId: 1, name: 'Laptop', category: 'Electronics', price: 999.99, stockQuantity: 10 },
    { productId: 2, name: 'Smartphone', category: 'Electronics', price: 699.99, stockQuantity: 25 },
    { productId: 3, name: 'Coffee Mug', category: 'Kitchen', price: 12.99, stockQuantity: 50 },
    { productId: 4, name: 'Desk Chair', category: 'Furniture', price: 149.99, stockQuantity: 8 },
    { productId: 5, name: 'Wireless Mouse', category: 'Electronics', price: 29.99, stockQuantity: 35 }
  ];

  private readonly productsSubject = new BehaviorSubject<Product[]>(this.products);
  private nextId = 6; // Track next available ID

  constructor() {}

  // Observable for components to subscribe to product changes
  getProducts(): Observable<Product[]> {
    return this.productsSubject.asObservable();
  }

  // Get all products synchronously
  getAllProducts(): Product[] {
    return [...this.products];
  }

  // Get product by ID
  getProductById(id: number): Product | undefined {
    return this.products.find(product => product.productId === id);
  }

  // Create new product
  createProduct(request: CreateProductRequest): Product {
    const newProduct: Product = {
      productId: this.nextId++,
      name: request.name,
      category: request.category,
      price: request.price,
      stockQuantity: request.stockQuantity
    };

    this.products.push(newProduct);
    this.productsSubject.next([...this.products]);
    return newProduct;
  }

  // Update existing product
  updateProduct(request: UpdateProductRequest): Product | null {
    const index = this.products.findIndex(p => p.productId === request.productId);
    
    if (index !== -1) {
      this.products[index] = { ...request };
      this.productsSubject.next([...this.products]);
      return this.products[index];
    }
    
    return null;
  }

  // Delete product by ID
  deleteProduct(id: number): boolean {
    const index = this.products.findIndex(p => p.productId === id);
    
    if (index !== -1) {
      this.products.splice(index, 1);
      this.productsSubject.next([...this.products]);
      return true;
    }
    
    return false;
  }

  // Get products by category
  getProductsByCategory(category: string): Product[] {
    return this.products.filter(p => 
      p.category.toLowerCase().includes(category.toLowerCase())
    );
  }

  // Search products by name
  searchProducts(searchTerm: string): Product[] {
    return this.products.filter(p => 
      p.name.toLowerCase().includes(searchTerm.toLowerCase())
    );
  }

  // Get all unique categories
  getCategories(): string[] {
    const categories = this.products.map(p => p.category);
    return [...new Set(categories)].sort();
  }

  // Get products with low stock (less than specified quantity)
  getLowStockProducts(threshold: number = 10): Product[] {
    return this.products.filter(p => p.stockQuantity < threshold);
  }

  // Calculate total inventory value
  getTotalInventoryValue(): number {
    return this.products.reduce((total, product) => 
      total + (product.price * product.stockQuantity), 0
    );
  }

  // Update stock quantity
  updateStock(productId: number, newQuantity: number): boolean {
    const product = this.products.find(p => p.productId === productId);
    
    if (product) {
      product.stockQuantity = newQuantity;
      this.productsSubject.next([...this.products]);
      return true;
    }
    
    return false;
  }
}
