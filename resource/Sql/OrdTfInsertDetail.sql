-- OrdTfInsertDetail : insert one order line (EV-ORD-308 step 3-3, 4-3. RowVersion is not listed: DEFAULT 1)
INSERT INTO [Order Details]
  (OrderID, ProductID, UnitPrice, Quantity, Discount)
VALUES
  (@OrderID, @ProductID, @UnitPrice, @Quantity, @Discount)
