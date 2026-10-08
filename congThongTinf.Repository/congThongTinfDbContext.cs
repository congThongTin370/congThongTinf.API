using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace congThongTinf.Domain
{
    public class congThongTinfDbContext : DbContext
    {
        public congThongTinfDbContext()
        {
        }

        public congThongTinfDbContext(DbContextOptions<congThongTinfDbContext> options)
            : base(options)
        {
        }
    }

}
