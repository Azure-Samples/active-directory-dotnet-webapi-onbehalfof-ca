using System;
using System.ComponentModel.DataAnnotations;

namespace TodoListService.DAL
{

    public class PerWebUserCache
    {
        [Key]
        public int EntryId { get; set; }
        public string webUserUniqueId { get; set; }
        public byte[] cacheBits { get; set; }
        public DateTime LastWrite { get; set; }
    }

    public class DbTokenCache 
    {
        private TodoListServiceContext db = new TodoListServiceContext();
        string User;
        PerWebUserCache Cache;
    }
}
