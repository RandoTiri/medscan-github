using MedScan.Api.Models;
using MedScan.Shared.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace MedScan.Api.Data;

public class AppDbContext : IdentityDbContext<ApplicationUser> {
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) {
  
    }
    //public DbSet<AppUser> AppUsers { get; set; }
}