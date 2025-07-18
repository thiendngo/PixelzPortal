﻿using Microsoft.EntityFrameworkCore;
using PixelzPortal.Application.Interfaces;
using PixelzPortal.Domain.Entities;
using PixelzPortal.Infrastructure.Persistence;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PixelzPortal.Infrastructure.Repository
{
    public class ProductionQueueRepository : IProductionQueueRepository
    {
        private readonly AppDbContext _db;

        public ProductionQueueRepository(AppDbContext db)
        {
            _db = db;
        }

        public async Task<List<ProductionQueue>> GetAllUnresolvedAsync()
        {
            return await _db.ProductionQueue
                .Where(q => !q.IsResolved)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();
        }
    }
}
