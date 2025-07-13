-- ApiTest Demo Database Initialization Script
-- This script sets up the test database and sample data for integration tests

-- Create the test database if it doesn't exist
--
CREATE DATABASE IF NOT EXISTS testdb;
USE testdb;

-- Create jobs table to match the existing API structure
--
CREATE TABLE IF NOT EXISTS jobs (
    id          INT PRIMARY KEY AUTO_INCREMENT,      -- Unique identifier
    title       VARCHAR(200) NOT NULL,               -- Job title
    description TEXT,                                -- Job description
    company     VARCHAR(100) NOT NULL,               -- Company name
    location    VARCHAR(100),                        -- Job location
    salary      DECIMAL(10,2),                       -- Salary amount
    created_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP, -- Record creation time
    updated_at  TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP
);

-- Create users table for additional test scenarios
--
CREATE TABLE IF NOT EXISTS users (
    id         INT PRIMARY KEY AUTO_INCREMENT,      -- Unique identifier
    name       VARCHAR(100) NOT NULL,               -- User's full name
    email      VARCHAR(100) NOT NULL UNIQUE,        -- User's email address
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP  -- Record creation time
);

-- Insert test job data for integration tests
--
INSERT INTO jobs (title, description, company, location, salary) VALUES 
('Software Engineer', 'Develop and maintain web applications', 'TechCorp', 'San Francisco, CA', 120000.00),
('Data Analyst', 'Analyze business data and create reports', 'DataCorp', 'New York, NY', 85000.00),
('DevOps Engineer', 'Manage CI/CD pipelines and infrastructure', 'CloudCorp', 'Seattle, WA', 110000.00),
('Product Manager', 'Lead product development and strategy', 'StartupCorp', 'Austin, TX', 130000.00);

-- Insert test user data for integration tests
--
INSERT INTO users (name, email) VALUES 
('John Doe', 'john.doe@example.com'),
('Jane Smith', 'jane.smith@example.com'),
('TestContainers User', 'testcontainers@example.com'),
('API Test User', 'apitest@example.com');
