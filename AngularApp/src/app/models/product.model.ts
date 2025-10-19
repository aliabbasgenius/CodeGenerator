export interface Product {
  productId: number;
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
}

export interface CreateProductRequest {
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
}

export interface UpdateProductRequest {
  productId: number;
  name: string;
  category: string;
  price: number;
  stockQuantity: number;
}