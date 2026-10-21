using System;
using System.Collections.Generic;
using System.Linq;
using CapaDatos;
using Entidades;

namespace CapaNegocio
{
    public class MorosidadService
    {
        private readonly MorosidadRepository _repository = new MorosidadRepository();

        public List<MorosidadInfo> ObtenerMorosos()
        {
            // AQUÍ ESTÁ LA CORRECCIÓN: Le pasamos la fecha exacta de Windows al repositorio
            return _repository.ObtenerMorosos(DateTime.Now.Date);
        }

        // Limpia el número para enviarlo a la API de WhatsApp
        public string FormatearTelefonoWhatsapp(string telefono)
        {
            string num = new string(telefono.Where(char.IsDigit).ToArray());
            if (num.StartsWith("0"))
                num = "58" + num.Substring(1); // Cambia el 0 por 58 (Código de Venezuela)
            return num;
        }
    }
}