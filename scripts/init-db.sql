-- init-db.sql
-- Run this script on first database creation

-- Create extension for UUID generation
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

-- Create extension for JSONB operations
CREATE EXTENSION IF NOT EXISTS "pgcrypto";

-- Create extension for Citus (distributed database)
CREATE EXTENSION IF NOT EXISTS "citus";

-- Create tenants table (shared)
CREATE TABLE IF NOT EXISTS public.tenants (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(255) NOT NULL,
    subdomain VARCHAR(255) UNIQUE NOT NULL,
    email VARCHAR(255) NOT NULL,
    tier VARCHAR(50) NOT NULL DEFAULT 'basic',
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    stripe_customer_id VARCHAR(255),
    stripe_subscription_id VARCHAR(255),
    subscription_start TIMESTAMP,
    subscription_end TIMESTAMP,
    settings JSONB DEFAULT '{}',
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create users table (shared) — platform login accounts (admin/portal/POS staff),
-- distinct from the per-tenant `employees` table created in create_tenant_schema() below.
CREATE TABLE IF NOT EXISTS public.users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_id UUID REFERENCES public.tenants(id),
    email VARCHAR(255) UNIQUE NOT NULL,
    password_hash VARCHAR(255) NOT NULL,
    first_name VARCHAR(100) NOT NULL,
    last_name VARCHAR(100) NOT NULL,
    phone VARCHAR(20),
    is_active BOOLEAN DEFAULT true,
    email_verified BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create refresh_tokens table (shared) — JWT refresh tokens issued by Identity Service.
-- Tokens are stored hashed; the raw token is only ever seen by the client.
CREATE TABLE IF NOT EXISTS public.refresh_tokens (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
    token_hash VARCHAR(255) UNIQUE NOT NULL,
    expires_at TIMESTAMP NOT NULL,
    revoked_at TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create roles table (shared)
CREATE TABLE IF NOT EXISTS public.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL,
    description TEXT,
    is_system_role BOOLEAN DEFAULT false,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create permissions table (shared)
CREATE TABLE IF NOT EXISTS public.permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    resource VARCHAR(100) NOT NULL,
    action VARCHAR(50) NOT NULL,
    description TEXT,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    UNIQUE(resource, action)
);

-- Create role_permissions (shared)
CREATE TABLE IF NOT EXISTS public.role_permissions (
    role_id UUID REFERENCES public.roles(id) ON DELETE CASCADE,
    permission_id UUID REFERENCES public.permissions(id) ON DELETE CASCADE,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (role_id, permission_id)
);

-- Create user_roles (shared)
CREATE TABLE IF NOT EXISTS public.user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL,
    role_id UUID NOT NULL REFERENCES public.roles(id),
    tenant_id UUID NOT NULL REFERENCES public.tenants(id),
    branch_id UUID,
    is_active BOOLEAN DEFAULT true,
    valid_from TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    valid_to TIMESTAMP,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);

-- Create reference tables
CREATE TABLE IF NOT EXISTS public.countries (
    id SERIAL PRIMARY KEY,
    code VARCHAR(2) UNIQUE NOT NULL,
    name VARCHAR(100) NOT NULL
);

CREATE TABLE IF NOT EXISTS public.business_types (
    id SERIAL PRIMARY KEY,
    name VARCHAR(50) UNIQUE NOT NULL,
    description TEXT
);

-- Insert reference data
INSERT INTO public.business_types (name, description) VALUES
('pharmacy', 'Pharmacy and healthcare products'),
('clothing', 'Clothing and fashion retail'),
('restaurant', 'Food and beverage services'),
('supershop', 'Supermarket and general retail');

-- Insert default system roles
INSERT INTO public.roles (name, description, is_system_role) VALUES
('Super Admin', 'Full system access', true),
('Subsidiary Manager', 'Manage entire business unit', true),
('Branch Manager', 'Full management of single branch', true),
('Senior Cashier', 'POS + inventory management', true),
('Cashier', 'Basic POS operations', true),
('Pharmacist', 'Pharmacy operations', true),
('Accountant', 'Financial operations', true);

-- Insert default permissions
INSERT INTO public.permissions (resource, action, description) VALUES
('sales', 'view', 'View sales transactions'),
('sales', 'create', 'Process new sales'),
('sales', 'edit', 'Modify existing sales'),
('sales', 'void', 'Cancel/void transactions'),
('sales', 'approve_refund', 'Approve customer refunds'),
('sales', 'apply_discount', 'Apply discounts'),
('inventory', 'view', 'View inventory levels'),
('inventory', 'adjust_stock', 'Adjust stock levels'),
('employees', 'view', 'View employee details'),
('employees', 'manage_schedule', 'Manage work schedules');

-- Indexes for users / refresh_tokens
CREATE INDEX IF NOT EXISTS idx_users_tenant ON public.users(tenant_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON public.refresh_tokens(user_id);
CREATE INDEX IF NOT EXISTS idx_refresh_tokens_expires ON public.refresh_tokens(expires_at);

-- Mark reference tables for Citus replication
SELECT create_reference_table('countries');
SELECT create_reference_table('business_types');
SELECT create_reference_table('tenants');
SELECT create_reference_table('users');
SELECT create_reference_table('roles');
SELECT create_reference_table('permissions');
SELECT create_reference_table('role_permissions');
SELECT create_reference_table('user_roles');
SELECT create_reference_table('refresh_tokens');

-- Function to create tenant schema dynamically
CREATE OR REPLACE FUNCTION create_tenant_schema(tenant_id TEXT)
RETURNS VOID AS $$
DECLARE
    schema_name TEXT;
BEGIN
    -- Build schema name
    schema_name := 'tenant_' || tenant_id;
    
    -- Create schema if it doesn't exist
    EXECUTE format('CREATE SCHEMA IF NOT EXISTS %I', schema_name);
    
    -- Set search path
    EXECUTE format('SET search_path TO %I', schema_name);
    
    -- Create tables within the schema
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.products (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            sku VARCHAR(100) NOT NULL,
            name VARCHAR(255) NOT NULL,
            description TEXT,
            base_price DECIMAL(10,2) NOT NULL,
            business_type VARCHAR(50) NOT NULL,
            category VARCHAR(100),
            attributes JSONB DEFAULT ''{}''::jsonb,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            UNIQUE(tenant_id, sku)
        )
    ', schema_name);
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.inventory (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            product_id UUID NOT NULL REFERENCES %I.products(id),
            branch_id UUID NOT NULL,
            quantity INT NOT NULL DEFAULT 0,
            min_stock_level INT DEFAULT 5,
            max_stock_level INT DEFAULT 1000,
            last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_by UUID,
            UNIQUE(tenant_id, product_id, branch_id)
        )
    ', schema_name, schema_name);
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.orders (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            branch_id UUID NOT NULL,
            order_number VARCHAR(50) UNIQUE NOT NULL,
            customer_id UUID,
            status VARCHAR(50) DEFAULT ''pending'',
            subtotal DECIMAL(10,2) NOT NULL,
            discount DECIMAL(10,2) DEFAULT 0,
            tax DECIMAL(10,2) DEFAULT 0,
            total DECIMAL(10,2) NOT NULL,
            payment_method VARCHAR(50),
            payment_status VARCHAR(50) DEFAULT ''pending'',
            created_by UUID,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ', schema_name);
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.order_items (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            order_id UUID NOT NULL REFERENCES %I.orders(id),
            product_id UUID NOT NULL REFERENCES %I.products(id),
            quantity INT NOT NULL,
            unit_price DECIMAL(10,2) NOT NULL,
            total_price DECIMAL(10,2) NOT NULL,
            discount DECIMAL(10,2) DEFAULT 0,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ', schema_name, schema_name, schema_name);
    
    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.payments (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            order_id UUID NOT NULL REFERENCES %I.orders(id),
            amount DECIMAL(10,2) NOT NULL,
            method VARCHAR(50) NOT NULL,
            status VARCHAR(50) NOT NULL DEFAULT ''pending'',
            transaction_reference VARCHAR(255),
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ', schema_name, schema_name);

    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.notifications (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            order_id UUID,
            channel VARCHAR(50) NOT NULL,
            recipient VARCHAR(255) NOT NULL,
            message TEXT NOT NULL,
            status VARCHAR(50) NOT NULL DEFAULT ''Pending'',
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            sent_at TIMESTAMP
        )
    ', schema_name);

    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.prescriptions (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            patient_id VARCHAR(255) NOT NULL,
            patient_name VARCHAR(255) NOT NULL,
            product_id UUID,
            medication_name VARCHAR(255) NOT NULL,
            quantity INTEGER NOT NULL,
            prescribed_by VARCHAR(255) NOT NULL,
            refills_allowed INTEGER NOT NULL DEFAULT 0,
            refills_used INTEGER NOT NULL DEFAULT 0,
            is_controlled_substance BOOLEAN NOT NULL DEFAULT false,
            requires_pharmacist_approval BOOLEAN NOT NULL DEFAULT false,
            status VARCHAR(50) NOT NULL DEFAULT ''Active'',
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            expires_at TIMESTAMP NOT NULL
        )
    ', schema_name);

    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.menu_items (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            name VARCHAR(255) NOT NULL,
            description TEXT,
            base_price DECIMAL(10,2) NOT NULL,
            category VARCHAR(100) NOT NULL,
            allergen_tags VARCHAR(500),
            is_available BOOLEAN NOT NULL DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ', schema_name);

    EXECUTE format('
        CREATE TABLE IF NOT EXISTS %I.employees (
            id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
            tenant_id UUID NOT NULL,
            branch_id UUID NOT NULL,
            first_name VARCHAR(100) NOT NULL,
            last_name VARCHAR(100) NOT NULL,
            email VARCHAR(255) UNIQUE NOT NULL,
            phone VARCHAR(20),
            role VARCHAR(50) NOT NULL,
            pin_hash VARCHAR(255),
            salary DECIMAL(10,2),
            hire_date DATE,
            is_active BOOLEAN DEFAULT true,
            created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
            updated_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
        )
    ', schema_name);
    
    -- Create indexes
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_products_sku ON %I.products(sku)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_products_tenant ON %I.products(tenant_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_inventory_product ON %I.inventory(product_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_inventory_branch ON %I.inventory(branch_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_orders_tenant ON %I.orders(tenant_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_orders_branch ON %I.orders(branch_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_orders_status ON %I.orders(status)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_order_items_order ON %I.order_items(order_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_payments_order ON %I.payments(order_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_employees_tenant ON %I.employees(tenant_id)', schema_name, schema_name);
    EXECUTE format('CREATE INDEX IF NOT EXISTS idx_%I_employees_email ON %I.employees(email)', schema_name, schema_name);
    
    RAISE NOTICE 'Schema % created successfully', schema_name;
END;
$$ LANGUAGE plpgsql;