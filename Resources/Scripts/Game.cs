using System.Collections.Generic;
using System.Collections;
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
    [SerializeField] private bool isDown = true;
    private Vector3 positionOfFirstCell;

    private float startTime;
    private float dt = 9.0f;

    public List<GameObject> food;           //Пул фишек для их генерации на поле.

    //Генерируем фишки на поле в методе Start()
    void Start()
    {
        positionOfFirstCell = transform.GetChild(0).transform.position;

        fieldObjects = new GameObject[8,8];

        for (int i = 0; i < MaxHorizontal; i++)
        {
            for (int j = 0; j < MaxVertical; j++)
            {
                fieldObjects[i, j] = Instantiate(food[Random.Range(0, food.Count)], new Vector3(positionOfFirstCell.x + distanceBetweenCells * j, positionOfFirstCell.y - distanceBetweenCells * i, z0), Quaternion.identity);
                fieldObjects[i, j].GetComponent<Cake>().SetTarget = new Point(j, i);
            }
        }

        startTime = Time.time + dt;
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
        }
        else if (!isDown)
        {
            int count = 0;

            for (int i = 0; i < MaxVertical; i++)
            {
                for (int j = 0; j < MaxHorizontal; j++)
                {
                    float posX = fieldObjects[i, j].GetComponent<Cake>().GetX * distanceBetweenCells + transform.GetChild(0).transform.position.x;
                    float posY = -fieldObjects[i, j].GetComponent<Cake>().GetY * distanceBetweenCells + transform.GetChild(0).transform.position.y;

                    Vector3 newPos = new Vector3(posX, posY, z0);

                    if (fieldObjects[i,j] && fieldObjects[i, j].transform.position == newPos)
                    {
                        ++count;
                    }
                }
            }

            if (count == (MaxHorizontal * MaxVertical))
            {
                isDown = true;
            }
        }
        else
        {
            if (Time.time > startTime)
            {
                /*
                 * Изначально проверяем совпадения. Если совпадения не были найдены и был ход, то переставляем выбранные ячейки
                 * для обмена обратно на свои места. Проверка поля будет происходит в любом случае, даже если не было хода,
                 * так как в начале логического выражения стоит функция checkAllMatch().
                 */
                Debug.Log("!!!Проверяю совпадения и принимаю данные от пользователя!!!");
                if (!checkAllMatch())
                {
                    if (wasTurn)
                    {
                        isMovable = true;
                        wasTurn = false;

                        var temp = p0;
                        p0 = target;
                        target = temp;

                        swap(ref p0, ref target);

                        firstCake.GetComponent<Cake>().SetTarget = target;
                        secondCake.GetComponent<Cake>().SetTarget = p0;

                        return;
                    }
                }
                else
                {
                    wasTurn = false;
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

                                firstCake.GetComponent<Cake>().SetTarget = target;
                                secondCake.GetComponent<Cake>().SetTarget = p0;

                                isMovable = true;
                                wasTurn = true;
                            }

                            Destroy(Selector);
                        }
                    }
                }
            }
        }
    }

    IEnumerator DownPieces()
    {
        isDown = false;

        yield return new WaitForFixedUpdate();

        for (int i = 0; i < MaxVertical; i++)
        {
            for (int j = 0; j < MaxHorizontal; j++)
            {
                if (fieldObjects[i, j] == null)
                {
                    for (int i1 = i - 1; i1 >= 0; i1--)
                    {
                        if(fieldObjects[i1, j]) fieldObjects[i1, j].GetComponent<Cake>().SetTarget = new Point(j, i1 + 1);

                        var tempObj = fieldObjects[i1 + 1, j];
                        fieldObjects[i1 + 1, j] = fieldObjects[i1, j];
                        fieldObjects[i1, j] = tempObj;
                    }
                }
            }
        }

        for (int i = 0; i < MaxHorizontal; i++)
        {
            for (int j = 0; j < MaxVertical; j++)
            {
                if (fieldObjects[i, j] == null)
                {
                    fieldObjects[i, j] = Instantiate(food[Random.Range(0, food.Count)], new Vector3(positionOfFirstCell.x + j * distanceBetweenCells, positionOfFirstCell.y + distanceBetweenCells, z0), Quaternion.identity);
                    fieldObjects[i, j].GetComponent<Cake>().SetTarget = new Point(j, i);
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
        List<GameObject> angleCakes = new List<GameObject>();
        List<GameObject> horizontalCakes = new List<GameObject>();
        List<GameObject> verticalCakes = new List<GameObject>();
        
        bool resAngle = checkMatch(SearchOfMatch.angle, ref angleCakes);                //Проверяем пять фишек углом

        if (angleCakes.Count > 0)
        {
            for (int i = 0; i < angleCakes.Count; i++)
            {
                Destroy(angleCakes[i]);
            }

            angleCakes.Clear();
            StartCoroutine(DownPieces());

            return true;
        }

        bool resHorizontal = checkMatch(SearchOfMatch.horizontal, ref horizontalCakes);      //Проверяем фишки горизонтально.

        if (horizontalCakes.Count > 0)
        {
            for (int i = 0; i < horizontalCakes.Count; i++)
            {
                Destroy(horizontalCakes[i]);
            }

            horizontalCakes.Clear();
            StartCoroutine(DownPieces());

            return true;
        }

        bool resVertical = checkMatch(SearchOfMatch.vertical, ref verticalCakes);          //Проверяем фишки вертикально.

        if (verticalCakes.Count > 0)
        {
            for (int i = 0; i < verticalCakes.Count; i++)
            {
                Destroy(verticalCakes[i]);
            }
            
            verticalCakes.Clear();
            StartCoroutine(DownPieces());
            
            return true;
        }

        return false;
    }

    private bool checkMatch(SearchOfMatch method, ref List<GameObject> deletingCakes)
    {
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
                                if (matchCount > 2)
                                {
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);
                                }
                                if (matchCount > 3) deletingCakes.Add(fieldObjects[i, j - 3]);
                                if (matchCount > 4) deletingCakes.Add(fieldObjects[i, j - 4]);
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
                        if (matchCount > 2)
                        {
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);
                        }
                        if (matchCount > 3) deletingCakes.Add(fieldObjects[i, MaxHorizontal - 4]);
                        if (matchCount > 4) deletingCakes.Add(fieldObjects[i, MaxHorizontal - 5]);
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
                                if (matchCount > 2)
                                {
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);
                                }
                                if (matchCount > 3) deletingCakes.Add(fieldObjects[i - 3, j]);
                                if (matchCount > 4) deletingCakes.Add(fieldObjects[i - 4, j]);
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

                        if (matchCount > 2)
                        {
                            deletingCakes.Add(fieldObjects[MaxVertical - 1, j]);
                            deletingCakes.Add(fieldObjects[MaxVertical - 2, j]);
                            deletingCakes.Add(fieldObjects[MaxVertical - 3, j]);
                        }
                        if (matchCount > 3) deletingCakes.Add(fieldObjects[MaxVertical - 4, j]);
                        if (matchCount > 4) deletingCakes.Add(fieldObjects[MaxVertical - 5, j]);
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
                                
                                deletingCakes.Add(fieldObjects[i + 1, j - 2]);
                                deletingCakes.Add(fieldObjects[i + 2, j - 2]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);
                                
                                Debug.Log("Угол четвёртой четверти!");
                            }
                            else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;

                                deletingCakes.Add(fieldObjects[i + 1, j]);
                                deletingCakes.Add(fieldObjects[i + 2, j]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                Debug.Log("Угол третьей четверти!");
                            }
                            else if (i > 1)
                            {
                                if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;

                                    deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    Debug.Log("Угол первой четверти!");
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;
                                    isAngle = true;
                                    matchCount = 1;

                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

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

                                    deletingCakes.Add(fieldObjects[i + 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i + 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    Debug.Log("Угол четвёртой четверти!");
                                }
                                else if (fieldObjects[i + 1, j].name == fieldObjects[i, j].name && fieldObjects[i + 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i + 1, j]);
                                    deletingCakes.Add(fieldObjects[i + 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    Debug.Log("Угол третьей четверти!");
                                }
                                else if (i > 1)
                                {
                                    if (fieldObjects[i - 1, j - 2].name == fieldObjects[i, j].name && fieldObjects[i - 2, j - 2].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;

                                        deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                        deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                        deletingCakes.Add(fieldObjects[i, j]);
                                        deletingCakes.Add(fieldObjects[i, j - 1]);
                                        deletingCakes.Add(fieldObjects[i, j - 2]);

                                        Debug.Log("Угол первой четверти!");
                                    }
                                    else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                    {
                                        isMatch = true;

                                        deletingCakes.Add(fieldObjects[i - 1, j]);
                                        deletingCakes.Add(fieldObjects[i - 2, j]);
                                        deletingCakes.Add(fieldObjects[i, j]);
                                        deletingCakes.Add(fieldObjects[i, j - 1]);
                                        deletingCakes.Add(fieldObjects[i, j - 2]);

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

                            deletingCakes.Add(fieldObjects[i + 1, MaxHorizontal - 3]);
                            deletingCakes.Add(fieldObjects[i + 2, MaxHorizontal - 3]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                            Debug.Log("Угол четвёртой четверти!");
                        }
                        else if (fieldObjects[i + 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i + 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                        {
                            isMatch = true;

                            deletingCakes.Add(fieldObjects[i + 1, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i + 2, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                            deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                            Debug.Log("Угол третьей четверти!");
                        }
                        if (i > 1)
                        {
                            if (fieldObjects[i - 1, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 3].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;

                                deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 3]);
                                deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 3]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                                Debug.Log("Угол первой четвертиЙ");
                            }
                            else if (fieldObjects[i - 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                            {
                                isMatch = true;

                                deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                                deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

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

                                deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

                                Debug.Log("Угол первой четверти!");
                            }
                            else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                            {
                                isMatch = true;
                                isAngle = true;
                                matchCount = 1;

                                deletingCakes.Add(fieldObjects[i - 1, j]);
                                deletingCakes.Add(fieldObjects[i - 2, j]);
                                deletingCakes.Add(fieldObjects[i, j]);
                                deletingCakes.Add(fieldObjects[i, j - 1]);
                                deletingCakes.Add(fieldObjects[i, j - 2]);

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

                                    deletingCakes.Add(fieldObjects[i - 1, j - 2]);
                                    deletingCakes.Add(fieldObjects[i - 2, j - 2]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    Debug.Log("Угол первой четверти!");
                                }
                                else if (fieldObjects[i - 1, j].name == fieldObjects[i, j].name && fieldObjects[i - 2, j].name == fieldObjects[i, j].name)
                                {
                                    isMatch = true;

                                    deletingCakes.Add(fieldObjects[i - 1, j]);
                                    deletingCakes.Add(fieldObjects[i - 2, j]);
                                    deletingCakes.Add(fieldObjects[i, j]);
                                    deletingCakes.Add(fieldObjects[i, j - 1]);
                                    deletingCakes.Add(fieldObjects[i, j - 2]);

                                    Debug.Log("Угол второй четверти");
                                }
                            }

                            matchCount = 1;
                            continue;
                        }

                        ++matchCount;
                    }

                    if (fieldObjects[i - 1, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name && fieldObjects[i - 2, MaxHorizontal - 1].name == fieldObjects[i, MaxHorizontal - 1].name)
                    {
                        isMatch = true;
                        matchCount = 1;

                        deletingCakes.Add(fieldObjects[i - 1, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i - 2, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 1]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 2]);
                        deletingCakes.Add(fieldObjects[i, MaxHorizontal - 3]);

                        Debug.Log("Угол второй четверти");
                    }
                }

                break;
        }

        return isMatch;
    }

    //Функция, выводящая результат вертикального или горизонтального совпадения.
    private void Result(int i, int j, int count, ref List<GameObject> deletingCakes, string str)
    {
        if (count > 2)
        {
            deletingCakes.Add(fieldObjects[i, j]);
            deletingCakes.Add(fieldObjects[i, j - 1]);
            deletingCakes.Add(fieldObjects[i, j - 2]);
        }
        if (count > 3) deletingCakes.Add(fieldObjects[i, j - 3]);
        if (count > 4) deletingCakes.Add(fieldObjects[i, j - 4]);

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
