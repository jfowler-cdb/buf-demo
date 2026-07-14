-- Create "Releases" table
CREATE TABLE `Releases` (
  `Id` text NOT NULL,
  `Title` text NOT NULL,
  `Artist` text NOT NULL,
  `Label` text NOT NULL,
  `ReleaseDate` text NOT NULL,
  PRIMARY KEY (`Id`)
);
