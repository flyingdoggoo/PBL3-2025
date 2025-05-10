// File: Utils/PaginatedList.cs
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PBL3.Utils // *** ĐẢM BẢO ĐÚNG NAMESPACE NÀY ***
{
    public class PaginatedList<T> : List<T>
    {
        public int PageIndex { get; private set; }
        public int TotalPages { get; private set; }
        public int TotalCount { get; private set; }

        public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
        {
            PageIndex = pageIndex;
            TotalCount = count;
            TotalPages = (int)Math.Ceiling(count / (double)pageSize);
            // Đảm bảo pageIndex hợp lệ sau khi tính TotalPages
            if (PageIndex > TotalPages && TotalPages > 0) PageIndex = TotalPages;
            if (PageIndex < 1) PageIndex = 1;

            this.AddRange(items);
        }

        public bool HasPreviousPage => PageIndex > 1;
        public bool HasNextPage => PageIndex < TotalPages;

        public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
        {
            var count = await source.CountAsync();
            // Đảm bảo pageIndex và pageSize hợp lệ trước khi Skip/Take
            pageIndex = Math.Max(1, pageIndex);
            pageSize = Math.Max(1, pageSize);
            int totalPages = (int)Math.Ceiling(count / (double)pageSize);
            pageIndex = Math.Min(pageIndex, totalPages > 0 ? totalPages : 1); // Không để pageIndex lớn hơn totalPages

            var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
            return new PaginatedList<T>(items, count, pageIndex, pageSize);
        }
    }
}