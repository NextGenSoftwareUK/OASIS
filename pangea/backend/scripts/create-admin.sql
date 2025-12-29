-- Create admin account script
-- Run this SQL query against your PostgreSQL database

-- Option 1: Update existing user to admin (replace email with your user's email)
UPDATE users 
SET role = 'admin' 
WHERE email = 'your-email@example.com';

-- Option 2: If you know the user ID
UPDATE users 
SET role = 'admin' 
WHERE id = 'user-uuid-here';

-- Verify the update
SELECT id, email, username, role FROM users WHERE role = 'admin';



