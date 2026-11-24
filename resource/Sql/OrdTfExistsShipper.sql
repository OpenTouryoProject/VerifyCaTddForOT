-- OrdTfExistsShipper : does the shipper exist (VAL-EXISTS)
SELECT
  COUNT(*)
FROM
  Shippers
WHERE
  ShipperID = @ShipperID
