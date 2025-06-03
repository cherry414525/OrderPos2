using Dapper;
using OrderFoodPos.Models.Orders;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace OrderFoodPos.Repositories.Orders
{
    public class OrderRepository : BaseRepository<Order>
    {
        public OrderRepository(IDbConnection db) : base(db) { }

        //建立訂單
        public async Task CreateOrderAsync(Order order)
        {
            var sql = @"
INSERT INTO Orders (OrderId, StoreId, OrderTime, TotalAmount, Status, CreatedAt)
VALUES (@OrderId, @StoreId, @OrderTime, @TotalAmount, @Status, GETDATE())";

            await _dbConnection.ExecuteAsync(sql, order);
        }

        //新增訂單細項
        public async Task AddOrderItemsAsync(IEnumerable<OrderItem> items)
        {
            var sql = @"
INSERT INTO OrderItems (OrderId, MenuName, Quantity, UnitPrice, TotalPrice, CreatedAt)
VALUES (@OrderId, @MenuName, @Quantity, @UnitPrice, @TotalPrice, GETDATE())";

            await _dbConnection.ExecuteAsync(sql, items);
        }

        // 更新訂單狀態
        public async Task UpdateOrderStatusAsync(string orderId, string newStatus)
        {
            var sql = "UPDATE Orders SET Status = @Status, UpdatedAt = GETDATE() WHERE OrderId = @OrderId";
            await _dbConnection.ExecuteAsync(sql, new { OrderId = orderId, Status = newStatus });
        }

        // 新增付款紀錄
        public async Task AddPaymentAsync(Payment payment)
        {
            var sql = @"
INSERT INTO Payments (OrderId, TransactionId, PayTime, Amount, Method, Status)
VALUES (@OrderId, @TransactionId, @PayTime, @Amount, @Method, @Status)";
            await _dbConnection.ExecuteAsync(sql, payment);
        }


        public async Task<Order?> GetOrderByIdAsync(string orderId)
        {
            var sql = "SELECT * FROM Orders WHERE OrderId = @OrderId";
            return await GetAsync(sql, new { OrderId = orderId });
        }

        public async Task<IEnumerable<OrderItem>> GetOrderItemsAsync(string orderId)
        {
            var sql = "SELECT * FROM OrderItems WHERE OrderId = @OrderId";
            return await _dbConnection.QueryAsync<OrderItem>(sql, new { OrderId = orderId });
        }
    }
}

