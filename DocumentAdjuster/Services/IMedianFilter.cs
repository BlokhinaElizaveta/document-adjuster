using Kontur.Recognition.ImageCore;

namespace DocumentAdjuster.Services
{
    internal interface IMedianFilter
    {
        /// <summary>
        /// Применяет медианную фильтрацию к изображению
        /// </summary>
        /// <param name="image">Изображение</param>
        /// <param name="radius">Радиус окна</param>
        /// <returns>Изображение с примененным медианным фильтром</returns>
        KrecImage Apply(KrecImage image, int radius);
    }
}
