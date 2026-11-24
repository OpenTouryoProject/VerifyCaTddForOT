-- OrdTfShippers : shipper choices for SC-ORD-03 (CompanyName ascending)
SELECT
  ShipperID, CompanyName
FROM
  Shippers
ORDER BY
  CompanyName ASC, ShipperID ASC
