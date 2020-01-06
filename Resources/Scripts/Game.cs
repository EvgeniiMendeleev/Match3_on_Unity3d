using System.Collections.Generic;
using UnityEngine;

enum SearchOfMatch { horizontal, vertical, angle }      //Метод проверки совпадений.

/*
 * Класс Point отвечает за координату фишки
 * на игровом поле для их перемещения и нахождение допустимой клетки
 * для хода.
 */
public sealed class Point
{
    public Point(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public int GetX { get { return x; } }
    public int GetY { get { return y; } }

    public int SetX { set { x = value; } }
    public int SetY { set { y = value; } }

    private int x;
    private int y;
}

public sealed class Game : MonoBehaviour
{
    [SerializeField] [Range(0, 1)] private float speed;     //Скорость, с которой перемещаются фишки при обмене.
    private GameObject Selector;                            //Префаб селектора, чтобы пользователь видел, какую фишку он выбрал.
    private float z0 = -0.01f;                              //Нулевая координата z для фишек, чтобы они не сливались с фоном.
    private Point p0, target;                               //p0 - точка фишки, которую выбрали первой, target - точка фишки, которую выбрали для обмена с первой.
    
    private GameObject firstCake, secondCake;               //Префабы, необходимые для взаимодействия с фишками, которые выбрали.
    [SerializeField] private bool isMovable = false;        //Переменная, которая отвечает за действия перемещения объектов.
    [SerializeField] private bool wasTurn = false;          //Переменная, отвечающая за информацию о том: был ли сделан ход или нет.

    private const float distanceBetweenCells = 0.438f;          //Растояние между клетками, необходимое для расстановки фишек на поле.
    private const uint MaxHorizontal = 8, MaxVertical = 8;      //Размерность поля по вертикали и горизонтали.
    private GameObject[,] fieldObjects;                         //Поле с фишками.

    public List<GameObject> food;           //Пул фишек для их генерации на поле.

    //Генерируем фишки на поле в методе Start()
    void Start()
    {
        Vector3 positionOfFirstCell = transform.GetChild(0).transform.position;

        fieldObjects = new GameObject[8,8];

        for (int i = 0; i < MaxHorizontal; i++)
        {
            for (int j = 0; j < MaxVertical; j++)
            {
                fieldObjects[i, j] = Instantiate(food[Random.Range(1, 6) - 1], new Vector3(positionOfFirstCell.x + distanceBetweenCells * j, positionOfFirstCell.y - distanceBetweenCells * i, z0), Quaternion.identity);
            }
        }
    }

    //Основное взаимодействие пользователя с игрой в FixedUpdate().
    void FixedUpdate()
    {
        if (isMovable)      //Если выполнилось условие, что фишки можно двигать, то меняем их местами.
        {
            //Находим координаты второй фишки на сцене для фишки, которую мы выбрали первой, чтобы переместить её на позицию второй фишки.
            float posX = target.GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
            float posY = -target.GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

            Vector3 secondPosition = new Vector3(posX, posY, z0);

            //Находим координаты первой фишки на сцене для второй фишки.
            posX = p0.GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
            posY = -p0.GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

            Vector3 firstPosition = new Vector3(posX, posY, z0);

            //Меняем их местами, пока фишки не встанут на свои места.
            if (firstCake.transform.position == secondPosition && secondCake.transform.position == firstPosition)
            {
                isMovable = false;
            }
            else
            {
                //Меняем фишки местами по линейно зависимости.
                firstCake.transform.position = Vector3.Lerp(firstCake.transform.position, secondPosition, speed);
                secondCake.transform.position = Vector3.Lerp(secondCake.transform.position, firstPosition, speed);
            }
        }
        else
        {
            /*
             * Изначально проверяем совпадения. Если совпадения не были найдены и был ход, то переставляем выбранные ячейки
             * для обмена обратно на свои места. Проверка поля будет происходит в любом случае, даже если не было хода,
             * так как в начале логического выражения стоит функция checkMatchAll().
             */

            if (!checkAllMatch() && wasTurn)
            {
                isMovable = true;
                wasTurn = false;

                Point temp = p0;
                p0 = target;
                target = temp;

                swap(ref p0, ref target);

                return;
            }

            //Читаем данные от пользователя.
            if (Input.GetKeyDown(KeyCode.Mouse0))
            {
                //Проверяем, какой объект на поле пользователь задел.
                RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

                if (hit && hit.collider.tag == "Cake")
                {
                    //Получаем информацию о расположении фишки в массиве, через координаты объекта на сцене.
                    int i = System.Convert.ToInt32(Mathf.Abs(hit.collider.transform.position.y - transform.GetChild(0).transform.position.y) / distanceBetweenCells);
                    int j = System.Convert.ToInt32(Mathf.Abs(hit.collider.transform.position.x - transform.GetChild(0).transform.position.x) / distanceBetweenCells);

                    //Если ни одна фишка не выбрана, то выделяем ту, которую выбрали.
                    if (!Selector)
                    {
                        Selector = Instantiate(Resources.Load<GameObject>("Prefabs/selector"), hit.collider.transform.position, Quaternion.identity);

                        p0 = new Point(j, i);
                        firstCake = fieldObjects[i, j];
                    }
                    else
                    {
                        target = new Point(j, i);
                        secondCake = fieldObjects[i, j];

                        //Если фишка была выбрана, то проверяем на допустимость их перемещения и уничтожаем селектор.
                        if (AssetTouch(ref p0, ref target))
                        {
                            swap(ref p0, ref target);

                            isMovable = true;
                            wasTurn = true;
                        }

                        Destroy(Selector);
                    }
                }
            }
         }
    }

    //Простая функция обмена значений местами.
    private void swap(ref Point d0, ref Point d1)
    {
        GameObject temp = fieldObjects[d0.GetY, d0.GetX];
        fieldObjects[d0.GetY, d0.GetX] = fieldObjects[d1.GetY, d1.GetX];
        fieldObjects[d1.GetY, d1.GetX] = temp;
    }

    //Функция проверки совпадений.
    private bool checkAllMatch()
    {
        bool resAngle = checkMatch(SearchOfMatch.angle);                //Проверяем пять фишек углом.
        bool resHorizontal = checkMatch(SearchOfMatch.horizontal);      //Проверяем фишки горизонтально.
        bool resVertical = checkMatch(SearchOfMatch.vertical);          //Проверяем фишки вертикально.

        //Если хотя бы одно совпадение есть, то возвращаем истину.
        if (resAngle || resHorizontal || resVertical)
        {
            return true;
        }
        return false;
    }

    private bool checkMatch(SearchOfMatch method)
    {
        /*
         * !!!На заметку!!!
         * Объединить проверку угла с множеством if - ов в одну функцию.
         */

        bool isMatch = false;       //Было ли хотя бы одно совпадение

        switch (method)
        {
            //Проверка фишек по горизонтали.
            case SearchOfMatch.horizontal:

                for (int i = 0; i < MaxVertical; i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                            if (matchCount > 2)
                            {
                                isMatch = true;
                                Result(i, j, matchCount, "горизонтали");
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    //Если в конце строки у нас есть совпадение из фишек.
                    if (matchCount > 2)
                    {
                        isMatch = true;
                        Result(i, System.Convert.ToInt32(MaxHorizontal - 1), matchCount, "горизонтали");
                    }
                }

                break;

            //Проверка фишек вертикально.
            case SearchOfMatch.vertical:

                for (int j = 0; j < MaxHorizontal; j++)
                {
                    int matchCount = 1;

                    for (int i = 0; i < (MaxVertical - 1); i++)
                    {
                        if (fieldObjects[i, j].name != fieldObjects[i + 1, j].name)
                        {
                            if (matchCount > 2)
                            {
                                isMatch = true;
                                Result(j, i, matchCount, "вертикали");
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    //Если в конце столбца у нас есть совпадение из фишек.
                    if (matchCount > 2)
                    {
                        isMatch = true;
                        Result(j, System.Convert.ToInt32(MaxVertical - 1), matchCount, "вертикали");
                    }
                }

                break;

            //Проверка фишек на совпадение углом (только для пяти фишек!)
            case SearchOfMatch.angle:

                for (int i = 0; i < (MaxVertical - 2); i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        /* Данное условие нужно, если у нас может быть совпадение следующего вида:
                         * .................
                         * ..........5......
                         * ..........5......
                         * .....555555......
                         * .................
                         */
                        if ((j <= (MaxHorizontal - 2)) && (matchCount == 3) && (fieldObjects[i, j + 1].name == fieldObjects[i, j].name))
                        {
                            bool isAngle = false;

                            if (fieldObjects[i + 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i + 2, j - 2].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;
                                Debug.Log("Угол четвёртой четверти!");
                            }
                            else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;
                                Debug.Log("Угол третьей четверти!");
                            }
                            else if (i > 1)
                            {
                                if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;
                                    Debug.Log("Угол первой четверти!");
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;
                                    Debug.Log("Угол второй четверти");
                                }
                            }

                            if (!isAngle) --matchCount;
                        }

                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                            if (matchCount == 3)
                            {
                                if (fieldObjects[i + 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i + 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    Debug.Log("Угол четвёртой четверти!");
                                }
                                else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    Debug.Log("Угол третьей четверти!");
                                }
                                else if (i > 1)
                                {
                                    if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;
                                        Debug.Log("Угол первой четверти!");
                                    }
                                    else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;
                                        Debug.Log("Угол второй четверти");
                                    }
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    /* Если у нас в конце строки есть совпадение углом, то есть:
                     * ..............
                     * .............4
                     * .............4
                     * ...........444
                     * ..............
                     */
                    if (matchCount == 3)
                    {
                        if (fieldObjects[i + 1, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i + 2, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name)
                        {
                            isMatch = true;
                            Debug.Log("Угол четвёртой четверти!");
                        }
                        else if (fieldObjects[i + 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i + 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                        {
                            isMatch = true;
                            Debug.Log("Угол третьей четверти!");
                        }
                        if (i > 1)
                        {
                            if (fieldObjects[i - 1, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;
                                Debug.Log("Угол первой четвертиЙ");
                            }
                            else if (fieldObjects[i - 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;
                                Debug.Log("Угол второй четверти!");
                            }
                        }
                    }
                }

                /* Проверка оставшихся двух строк в поле на совпадения вида:
                 *  ...................
                 *  5..................
                 *  5..................
                 *  555................
                 */ 
                for (int i = System.Convert.ToInt32(MaxVertical - 2); i < MaxVertical; i++)
                {
                    int matchCount = 1;

                    for (int j = 0; j < (MaxHorizontal - 1); j++)
                    {
                        if ((j <= (MaxHorizontal - 2)) && (matchCount == 3) && (fieldObjects[i, j + 1].name == fieldObjects[i, j].name))
                        {
                            bool isAngle = false;

                            if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;
                                Debug.Log("Угол первой четверти!");
                            }
                            else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;
                                Debug.Log("Угол второй четверти");
                            }

                            if (!isAngle) --matchCount;
                        }
                        if (fieldObjects[i, j].name != fieldObjects[i, j + 1].name)
                        {
                            if (matchCount == 3)
                            {
                                if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    Debug.Log("Угол первой четверти!");
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    Debug.Log("Угол второй четверти");
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }
                }

                break;
        }

        return isMatch;
    }

    //Функция, выводящая результат вертикального или горизонтального совпадения.
    private void Result(int i, int j, int count, string str)
    {
        switch (count)
        {
            case 3:
                Debug.Log("Совпадение из 3 фишек по " + str + " в " + i + " строке!");
                Debug.Log("j3 = " + j + ", j2 = " + (j - 1) + ", j1 = " + (j - 2));

                break;
            case 4:
                Debug.Log("Совпадение из 4 фишек по " + str + " в " + i + " строке!");
                Debug.Log("j4 =" + j + ", j3 = " + (j - 1) + ", j2 = " + (j - 2) + ", j1 = " + (j - 3));
                break;
            case 5:
                Debug.Log("Совпадение из 5 фишек по " + str + " в " + i + " строке!");
                Debug.Log("j5 = " + j + ", j4 =" + (j - 1) + ", j3 = " + (j - 2) + ", j2 = " + (j - 3) + ", j1 = " + (j - 4));
                break;
        }
    }

    //Функция проверки на допустимость перемещения клеток со своими координатами p0 и target.
    private bool AssetTouch(ref Point p0, ref Point target)
    {
        if(Mathf.Abs(target.GetY - p0.GetY) == 0)           //Если фишки находятся на одной строке.
        {
            if(Mathf.Abs(target.GetX - p0.GetX) == 1)       //Если фишки находятся в разных столбцах, где они являются соседними.
            {
                return true;
            }
        }
        else if(Mathf.Abs(target.GetX - p0.GetX) == 0)      //Если фишки находятся в одном столбце.
        {
            if(Mathf.Abs(target.GetY - p0.GetY) == 1)       //Если фишки находятся на разных строк, где они являются соседними.
            {
                return true;
            }
        }

        return false;
    }
}