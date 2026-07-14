-- Add column "CreatedAt" to table: "Releases"
ALTER TABLE `Releases` ADD COLUMN `CreatedAt` text NOT NULL;
-- Add column "UpdatedAt" to table: "Releases"
ALTER TABLE `Releases` ADD COLUMN `UpdatedAt` text NOT NULL;
