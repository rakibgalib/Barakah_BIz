// TypeScript mirrors of the backend DTOs (System.Text.Json serializes records as camelCase).
// Sources:
//   Identity: src/Modules/IdentityService/Dtos.cs
//   Tenant:   src/Modules/TenantService/Dtos.cs
//   Catalog:  src/Modules/CatalogService/Dtos.cs
//   Inventory:src/Modules/InventoryService/Dtos.cs
//   Order:    src/Modules/OrderService/Dtos.cs
//   Payment:  src/Modules/PaymentService/Dtos.cs

// ---------- Identity ----------

export interface UserResponse {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  tenantId: string | null;
  emailVerified: boolean;
}

export interface AuthResponse {
  accessToken: string;
  refreshToken: string;
  accessTokenExpiresAt: string;
  user: UserResponse;
}

export interface RegisterRequest {
  email: string;
  password: string;
  firstName: string;
  lastName: string;
  phone: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshRequest {
  refreshToken: string;
}

// ---------- Tenant ----------

export interface Tenant {
  id: string;
  name: string;
  subdomain: string;
  email: string;
  tier: string;
  status: string;
  createdAt: string;
}

export interface CreateTenantRequest {
  name: string;
  subdomain: string;
  email: string;
  tier: string;
}

export interface UpdateTenantStatusRequest {
  status: string;
}

// ---------- Catalog ----------

export interface Product {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  basePrice: number;
  businessType: string;
  category: string | null;
  attributes: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateProductRequest {
  sku: string;
  name: string;
  description?: string | null;
  basePrice: number;
  businessType: string;
  category?: string | null;
  attributes?: string | null;
}

export interface UpdateProductRequest {
  name: string;
  description?: string | null;
  basePrice: number;
  category?: string | null;
  attributes?: string | null;
}

// ---------- Inventory ----------

export interface InventoryItem {
  id: string;
  productId: string;
  branchId: string;
  quantity: number;
  minStockLevel: number;
  maxStockLevel: number;
  lastUpdated: string;
}

export interface CreateInventoryItemRequest {
  productId: string;
  branchId: string;
  quantity: number;
  minStockLevel: number;
  maxStockLevel: number;
}

export interface AdjustInventoryRequest {
  delta: number;
  updatedBy?: string | null;
}

// ---------- Order ----------

export interface OrderItem {
  productId: string;
  quantity: number;
  unitPrice: number;
  totalPrice: number;
  discount: number;
}

export interface Order {
  id: string;
  orderNumber: string;
  branchId: string;
  customerId: string | null;
  status: string;
  subtotal: number;
  discount: number;
  tax: number;
  total: number;
  paymentMethod: string | null;
  paymentStatus: string;
  createdAt: string;
  items: OrderItem[];
}

export interface OrderItemRequest {
  productId: string;
  quantity: number;
}

export interface CreateOrderRequest {
  branchId: string;
  customerId?: string | null;
  items: OrderItemRequest[];
  paymentMethod?: string | null;
  createdBy?: string | null;
}

export interface UpdateOrderStatusRequest {
  status: string;
}

export interface UpdateOrderPaymentStatusRequest {
  paymentStatus: string;
}

// ---------- Payment ----------

export interface Payment {
  id: string;
  orderId: string;
  amount: number;
  method: string;
  status: string;
  transactionReference: string | null;
  createdAt: string;
}

export interface CreatePaymentRequest {
  orderId: string;
  amount: number;
  method: string;
  transactionReference?: string | null;
}

// ---------- Notification ----------

export interface Notification {
  id: string;
  tenantId: string;
  orderId: string | null;
  channel: string;
  recipient: string;
  message: string;
  status: string;
  createdAt: string;
  sentAt: string | null;
}

export interface CreateNotificationRequest {
  orderId?: string | null;
  channel: string;
  recipient: string;
  message: string;
}

// ---------- Analytics ----------

export interface SalesSummary {
  totalOrders: number;
  totalRevenue: number;
  avgOrderValue: number;
  totalPaymentsReceived: number;
  from: string;
  to: string;
}

// ---------- Pharmacy ----------

export interface Prescription {
  id: string;
  tenantId: string;
  patientId: string;
  patientName: string;
  productId: string;
  medicationName: string;
  quantity: number;
  prescribedBy: string;
  refillsAllowed: number;
  refillsUsed: number;
  isControlledSubstance: boolean;
  requiresPharmacistApproval: boolean;
  status: string;
  createdAt: string;
  expiresAt: string;
}

export interface CreatePrescriptionRequest {
  patientId: string;
  patientName: string;
  productId: string;
  medicationName: string;
  quantity: number;
  prescribedBy: string;
  refillsAllowed: number;
  isControlledSubstance: boolean;
  expiresAt: string;
}

// ---------- Restaurant ----------

export interface MenuItem {
  id: string;
  tenantId: string;
  name: string;
  description: string | null;
  basePrice: number;
  category: string;
  allergenTags: string | null;
  isAvailable: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMenuItemRequest {
  name: string;
  description?: string | null;
  basePrice: number;
  category: string;
  allergenTags?: string | null;
}

export interface PricedMenuItem {
  id: string;
  name: string;
  basePrice: number;
  currentPrice: number;
  pricingTier: string;
}

// ---------- SuperShop ----------

export interface Supplier {
  id: string;
  tenantId: string;
  name: string;
  contactEmail: string | null;
  contactPhone: string | null;
  rating: number | null;
  isActive: boolean;
  createdAt: string;
}

export interface CreateSupplierRequest {
  name: string;
  contactEmail?: string | null;
  contactPhone?: string | null;
}

export interface LoyaltyAccount {
  id: string;
  tenantId: string;
  customerId: string;
  pointsBalance: number;
  totalPointsEarned: number;
  createdAt: string;
  updatedAt: string;
}

export interface ProductBatch {
  id: string;
  tenantId: string;
  productId: string;
  batchNumber: string;
  quantity: number;
  expiresAt: string;
  createdAt: string;
}

export interface CreateProductBatchRequest {
  productId: string;
  batchNumber: string;
  quantity: number;
  expiresAt: string;
}

// ---------- Clothing ----------

export interface SustainabilityRating {
  id: string;
  tenantId: string;
  productId: string;
  score: number;
  certifications: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreateSustainabilityRatingRequest {
  productId: string;
  score: number;
  certifications?: string | null;
}
