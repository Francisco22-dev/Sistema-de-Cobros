# Constitución — Sistema de Cobros 2.0

1. **Stack:** C# + WPF (net8.0-windows). Sin dependencias ni frameworks externos.
2. **Spec primero:** El código implementa specs/ aprobadas; nada de funcionalidades no especificadas.
3. **Separación:** Entidades → CapaDatos → CapaNegocio → UI. La UI nunca accede a datos directamente.
4. **Tests:** Sin framework de tests instalado. Verificación manual documentada en MEMORY.md.
5. **Datos:** Cambios de formato requieren migración o compatibilidad. Nada de datos sensibles en memoria o logs.
6. **Idioma:** Interfaz y comentarios en español. Fechas siempre en hora local (DateTime.Now).
