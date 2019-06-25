
using System.Collections.Generic;

namespace ZelvParser
{
    /// <summary>
    /// class for book page from labirint
    /// </summary>
    public class Book
    {

        //Дата парсинга
        public string Date { get; set; }


        //Категория
        public string Category { get; set; }


        //заголовок статьи
        public string Title { get; set; }

        //текст статьи
        public string Text { get; set; }
        
        //длина текста статьи, для проверки
        public int TextLength { get; set; }

        //текст разбитый на части
        public string TextParts { get; set; }

        //длина частей,первая это сумма всех
        public string TextPartsLength { get; set; }

        //количество кусков текста разбитого на части
        public int TextPartsCount { get; set; }

        //теги к статье
        public string Tags { get; set; }

        //ссылка на статью
        public string Link { get; set; }

        //json объект, содержащий ссылки на картинку и ее название прогнанное через хэш функцию
        public string Pictures { get; set; }
        
        //Количество картинок, которые должны быть равны количеству частей текста.
        public int PicturesCount { get; set; }

        //json объект, содержащий ссылки на картинку и ее название прогнанное через хэш функцию
        public string PicturesList { get; set; }

        public string CategoryName { get; set; }

        //public string Price { get; set; }

        //public string InStock { get; set; }

        //public string Stars { get; set; }

        //public string Genre { get; set; }

        public Book() { }

        /// <summary>
        /// overrided method ToString(0 for error output
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            
            return string.Format("\n\n" 
                + this.Link
                + "\n" + this.Title
                + "\n\n");
        }
    }
}
