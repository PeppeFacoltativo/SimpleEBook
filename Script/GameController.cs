using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameController : MonoBehaviour
{
    //RIVEDI DA ZERO FUNZIONAMENTO SLIDER #TODO

    #region Declarations

        #region VisibleSettings
        [Tooltip("Path of the folder containing the images")]
        public string source;
        [Tooltip("True if the folder source contains Gameobjects, not images")]
        public bool filesAreGameobjects;

        [Header("Touch Feedback Settings")]
        [Tooltip("Movement before the page transition")]
        public int brosweMagnitude;
        [Tooltip("Movement before the beginning of the drag")]
        public int dragMagnitude;
        [Tooltip("Page transition speed after finger released")]
        public int speed;
        [Tooltip("Feedback during the drag")]
        public int dragResistance;
        #endregion

        #region Slider
        [Header("Slider Settings")]
        [Tooltip("Gameobject Slider")]
        public GameObject slider;
        [Tooltip("slider position while not active (Off screen)")]
        public Vector2 sliderIdlePosition;
        [Tooltip("slider position while active (on screen)")]
        public Vector2 sliderActivePosition;
        [Tooltip("Transition speed of the slider")]
        public int sliderSpeed;
        [Tooltip("Time before the slider disappears when not touched")]
        public float sliderFadeTime;

        int sliderMagnitude; //minimum movement (in pixel) before slider appears (=magnitude)
        bool sliderActive; 
        bool lerpSlider;
        #endregion

        #region Utility
        GameObject[] pages;
        GameObject currentPage;
        float[] goal;
        bool lerp;
        int page;
        Vector2 startPosition;
        float timerSlider;
        RectTransform canva;
        const float lerpApprox = 0.005f;
        #endregion

    #endregion

    #region Initializations
    // Use this for initialization
    void Awake()
    {
        page = 0;
        lerp = false;
        lerpSlider = false;
        sliderActive = false;
        sliderMagnitude = dragMagnitude;  
    }

    private void Start()
    {
        GameObject[] images = null;
        Sprite[] imagesSprite = null;
        if (filesAreGameobjects)
            images = Resources.LoadAll<GameObject>(source) as GameObject[];
        else
            imagesSprite = Resources.LoadAll<Sprite>(source) as Sprite[];

        canva = transform.parent.GetComponent<RectTransform>();

        if (filesAreGameobjects)
            pages = new GameObject[images.Length];
        else
            pages = new GameObject[imagesSprite.Length];

        //Images instantiation
        for (int i = 0; i < images.Length; i++)
        {
            GameObject tempImage = new GameObject();
            if (filesAreGameobjects)
                tempImage = Instantiate(images[i], transform);
            else
            {
                tempImage = Instantiate(tempImage, transform);
                tempImage.GetComponent<Image>().sprite = imagesSprite[i];
            }
            tempImage.name = "img" + (i + 1);
            pages[i] = tempImage;
        }

        //Scale Navigation Index
        ResetGoals(pages.Length);

        currentPage = pages[0];

        //Optimization
        DisableImages(0);

        //set slider value
        slider.GetComponent<Slider>().maxValue = pages.Length - 1;
    }
    #endregion

    // Update is called once per frame
    void Update()
    {
        #region swipeAndTapDetection
        //check touch phase
        Vector2 touchDelta;
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            startPosition = Input.GetTouch(0).position;

        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Moved)
        {
            //drag handler
            lerp = false;
            touchDelta = Input.GetTouch(0).deltaPosition;
            Vector2 deltaFromStart = Input.GetTouch(0).position - startPosition;

            //Slows drag effect
            float movement = touchDelta.x / dragResistance;

            //if I moved my finger enough the drag begins
            if (Mathf.Abs(deltaFromStart.x) > dragMagnitude)
            {
                //The first page doesn't move back and the last doesn't move forward
                if (page == 0 && movement < 0)
                {
                    if (transform.position.x + movement >= goal[page + 1])
                        transform.Translate(movement, 0, 0);
                    else
                        transform.position = new Vector3(goal[page + 1], transform.position.y, transform.position.z);
                }
                else if (page == goal.Length - 1 && movement > 0)
                {
                    if (transform.position.x + movement <= goal[page - 1])
                        transform.Translate(movement, 0, 0);
                    else
                        transform.position = new Vector3(goal[page - 1], transform.position.y, transform.position.z);
                }
                else
                {
                    //I can just to the page before and after the current
                    if (transform.position.x + movement >= goal[page + 1])
                    {
                        if (transform.position.x + movement <= goal[page - 1])
                            transform.Translate(movement, 0, 0);
                        else
                            transform.position = new Vector3(goal[page - 1], transform.position.y, transform.position.z);
                    }
                    else
                        transform.position = new Vector3(goal[page + 1], transform.position.y, transform.position.z);
                }
            }
        }
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended)
        {
            Vector2 endPosition = Input.GetTouch(0).position;
            Vector2 deltaPosition = endPosition - startPosition;
            //My finger has moved of at least <brosweMagnitude> pixels
            if (Mathf.Abs(deltaPosition.x) > brosweMagnitude)
            {
                if (deltaPosition.x > brosweMagnitude)
                    PreviousPage();
                if (deltaPosition.x < -brosweMagnitude)
                    NextPage();
                slider.GetComponent<Slider>().value = page;
            }
            else
            {
                lerp = true;
                //Swipe up: show slider
                if (deltaPosition.y > sliderMagnitude && !sliderActive)
                    lerpSlider = true;
            }
        }
        #endregion

        #region SwipeFromPC
        //FOR DEBUG 
        if (Input.GetKeyDown(KeyCode.RightArrow))
            NextPage();
        if (Input.GetKeyDown(KeyCode.LeftArrow))
            PreviousPage();
        if (Input.GetKeyDown(KeyCode.UpArrow)&&!sliderActive)
            lerpSlider = true;
        #endregion

        #region lerp
        if (lerp)
        {
            //Last page
            if (page == goal.Length)
                page--;
            //First page
            if (page < 0)
                page = 0;
            transform.position = Vector3.Lerp(transform.position, new Vector3(goal[page], transform.position.y, transform.position.z), speed * Time.deltaTime);
            //If the distance between goal and page is < lerpApprox I complete the lerp manually
            float dist = Vector3.Distance(transform.position, new Vector3(goal[page], transform.position.y, transform.position.z));
            if (dist <= lerpApprox)
            {
                if (transform.position.x > goal[page])
                    dist = -dist;
                transform.Translate(dist, 0, 0);
                lerp = false;
            }
        }
        #endregion

        #region slider
        if (lerpSlider)
            ShowSlider();

        if (sliderActive)
        {
            timerSlider -= Time.deltaTime;
            //The slider hides
            if (timerSlider <= 0)
                HideSlider();
        }
        #endregion
    }

    #region functions

    private void ResetGoals(int newLength)
    {
        goal = null;
        goal = new float[newLength];
        for (int i = 0; i < newLength; i++)
            goal[i] = -i * (canva.localScale.x * canva.rect.width);
    }

    private void DisableImages(int currentPage)
    {
        for (int i = 0; i < pages.Length; i++)
        {
            //Just two pages before and after the current page are active
            if (i == currentPage - 2 || i == currentPage - 1 || i == currentPage || i == currentPage + 1 || i == currentPage + 2)
            {
                if (currentPage - 2 == -2 && currentPage - 1 == -1 && i == currentPage - 1 && i == currentPage - 2) { }
                else if (currentPage + 1 == pages.Length && currentPage + 1 == pages.Length + 1 && i == currentPage + 1 && i == currentPage + 2) { }
                else
                    pages[i].GetComponent<Image>().enabled = true;
            }
            else
                pages[i].GetComponent<Image>().enabled = false;
        }
    }

    public void sliderMovement()
    {
        timerSlider = sliderFadeTime;
        page = (int)slider.GetComponent<Slider>().value;
        lerp = true;
        DisableImages(page);
        currentPage = pages[page];
    }

    public void PreviousPage()
    {
        lerp = true;
        if (page > 0)
        {
            page--;
            currentPage = pages[page];
        }
        DisableImages(page);
    }

    public void NextPage()
    {
        lerp = true;
        if (page < pages.Length - 1)
        {
            page++;
            currentPage = pages[page];
        }
        else if (page == pages.Length - 1)
        {
            //DECIDE WHAT TO DO IF I SWIPE LEFT IN THE LAST PAGE
        }
        DisableImages(page);
    }

    private void ShowSlider()
    {
        slider.transform.position = Vector2.Lerp(slider.transform.position, sliderActivePosition, sliderSpeed * Time.deltaTime);
        if (Vector2.Distance(slider.transform.position, sliderActivePosition) <= 0.02)
        {
            slider.transform.position = sliderActivePosition;
            lerpSlider = false;
            sliderActive = true;
            timerSlider = sliderFadeTime;
        }
    }

    private void HideSlider()
    {
        slider.transform.position = Vector2.Lerp(slider.transform.position, sliderIdlePosition, sliderSpeed * Time.deltaTime);
        if (Vector2.Distance(slider.transform.position, sliderIdlePosition) <= 0.02)
        {
            slider.transform.position = sliderIdlePosition;
            lerpSlider = false;
            sliderActive = false;
        }
    }

    #endregion
}

