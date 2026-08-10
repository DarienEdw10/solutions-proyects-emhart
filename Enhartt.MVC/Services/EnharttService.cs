using Enhartt.Domain.Models;
using Enhartt.Domain.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Enhartt.MVC.Services
{
    public class EnharttService
    {
        private readonly IRepository repository;

        public EnharttService(IRepository repository)

        {
            this.repository = repository;
        }
        public async Task<List<Maquina>> ObtenerMaquinasAsync()
        {
            IEnumerable<Maquina> maquinas = await repository.ObtenerMaquinasAsync();
            return maquinas.ToList();
        }
    }

}