using OrderFoodPos.Models.Orders;
using OrderFoodPos.Repositories.Orders;
using OrderFoodPos.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OrderFoodPos.Services.Orders
{
    public class OrderService
    {
        private readonly OrderRepository _orderRepository;

        public OrderService(OrderRepository orderRepository)
        {
            _orderRepository = orderRepository;
        }

        public async Task CreateOrderAsync(LinePayRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.orderId))
                throw new ArgumentException("無效的訂單資料");

            var order = new Order
            {
                OrderId = request.orderId,
                StoreId = 1, // 假設固定，未來可從 request 傳入
                OrderTime = DateTime.UtcNow,
                TotalAmount = request.amount,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };

            var items = new List<OrderItem>();
            foreach (var pack in request.packages)
            {
                foreach (var product in pack.products)
                {
                    items.Add(new OrderItem
                    {
                        OrderId = request.orderId,
                        MenuName = product.name, 
                        Quantity = product.quantity,
                        UnitPrice = product.price,
                        TotalPrice = product.price * product.quantity,
                        CreatedAt = DateTime.UtcNow
                    });
                }
            }

            await _orderRepository.CreateOrderAsync(order);
            await _orderRepository.AddOrderItemsAsync(items);
        }

        //更新狀態
        public async Task UpdateOrderStatusAsync(string orderId, string newStatus)
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, newStatus);
        }

        //新增付款資訊
        public async Task AddPaymentAsync(string orderId, string transactionId, int amount)
        {
            var payment = new Payment
            {
                OrderId = orderId,
                TransactionId = transactionId,
                PayTime = DateTime.UtcNow,
                Amount = amount,
                Method = "LinePay",
                Status = "Success"
            };

            await _orderRepository.AddPaymentAsync(payment);
        }


        public async Task<Order?> GetOrderAsync(string orderId)
        {
            return await _orderRepository.GetOrderByIdAsync(orderId);
        }
    }
}
