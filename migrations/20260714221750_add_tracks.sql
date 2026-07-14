-- Create "Tracks" table
CREATE TABLE `Tracks` (
  `Id` text NOT NULL,
  `Title` text NOT NULL,
  `Artist` text NOT NULL,
  `Duration` text NOT NULL,
  `TrackNumber` integer NOT NULL,
  `Isrc` text NOT NULL,
  `CreatedAt` text NOT NULL,
  `UpdatedAt` text NOT NULL,
  PRIMARY KEY (`Id`)
);
-- Create "ReleaseTracks" table
CREATE TABLE `ReleaseTracks` (
  `ReleaseId` text NOT NULL,
  `TrackId` text NOT NULL,
  PRIMARY KEY (`ReleaseId`, `TrackId`),
  CONSTRAINT `FK_ReleaseTracks_Tracks_TrackId` FOREIGN KEY (`TrackId`) REFERENCES `Tracks` (`Id`) ON UPDATE NO ACTION ON DELETE CASCADE,
  CONSTRAINT `FK_ReleaseTracks_Releases_ReleaseId` FOREIGN KEY (`ReleaseId`) REFERENCES `Releases` (`Id`) ON UPDATE NO ACTION ON DELETE CASCADE
);
-- Create index "IX_ReleaseTracks_TrackId" to table: "ReleaseTracks"
CREATE INDEX `IX_ReleaseTracks_TrackId` ON `ReleaseTracks` (`TrackId`);
