﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SocketIO;
using UnityEngine.SceneManagement;
using System;

public class GameController : MonoBehaviour
{
    public SocketIOComponent socket;
    public PlayerCon playerCon;
    public QuestionModel question;
    public ChoicesController choiceCon;

    public Slider timerBar;
    private float maxTime = 5f;
    private float timeLeft;

    public Text Scoretxt;

    public Transform choicesPanel;

    public Text questiontxt;
    public Text qnumbertxt;
    public Text correctTxt;
    public Text wrongTxt;

    public List<GameObject> choicesList = new List<GameObject>();
    public SimpleObjectPool answerButtonObjectPool;

    void Start()
    {
        if (socket != null)
        {
            socket.On("gameResult", showresult);
            socket.On("Q1", setgame);
            Debug.Log("Socket Connected");
            StartCoroutine("CallStart");
        }
        else
        {
            Debug.Log("Socket is null");
        }
    }

    public void showresult(SocketIOEvent obj)
    {
        SceneManager.LoadScene("Result");
    }

    public void setgame(SocketIOEvent obj)
    {
            correctTxt.gameObject.SetActive(false);
            wrongTxt.gameObject.SetActive(false);
            Debug.Log("Game Set" + obj.data);
            QuestionModel question = JsonUtility.FromJson<QuestionModel>(obj.data.ToString());
            Debug.Log(question.question);
            qnumbertxt.text = "Question " + question.id;
            questiontxt.text = question.question;
            List<QuestionModel.Choices> choices = new List<QuestionModel.Choices>();
            choices = question.choices;
            choices = ShuffleList(choices);
            RemoveChoicesButton();

            foreach (var item in choices)
            {
                GameObject choicesController = answerButtonObjectPool.GetObject();
                choicesController.transform.SetParent(choicesPanel);
                choicesList.Add(choicesController);
                ChoicesController choice = choicesController.GetComponent<ChoicesController>();
                choice.Setup(item);
                timeLeft = maxTime;
                choice.Isclick = false;
                Button ans = choice.GetComponent<Button>();
                ans.onClick.AddListener(() => {
                    if (choice.Isclick == false)
                    {
                        Debug.Log("Onclick" + choice.choices.text);
                        if (choice.choices.value == true)
                        {
                            Debug.Log("Correct");
                            choice.Isclick = true;
                            correctTxt.gameObject.SetActive(true);
                            var data = JsonUtility.ToJson(playerCon.player);
                            socket.Emit("Answer", new JSONObject(data));
                            Debug.Log("Sending");
                        }
                        else
                        {
                            Debug.Log("Wrong");
                            choice.Isclick = true;
                            wrongTxt.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        Debug.Log("Cannot ans again!!");
                    }

                });
            }
    }

    private void RemoveChoicesButton()
    {
        while (choicesList.Count > 0)
        {
            answerButtonObjectPool.ReturnObject(choicesList[0].gameObject);
            choicesList.RemoveAt(0);
        }
    }

    public IEnumerator CallStart()
    {
        yield return new WaitForSeconds(0.3f);
        var data = JsonUtility.ToJson(playerCon.player);
        Debug.Log(data);
        socket.Emit("LoadQ", new JSONObject(data));
        Debug.Log("Sending");
    }

    void Update()
    {
        timerBar.value = CalculateSliderValue();
        if (timeLeft>0)
        {
            timeLeft -= Time.deltaTime;
        }
        else if(timeLeft<=0)
        {
            timeLeft = 0;
        }
    }

    public float CalculateSliderValue()
    {
        return (timeLeft / maxTime);
    }

    private List<E> ShuffleList<E>(List<E> inputList)
    {
        List<E> randomList = new List<E>();

        System.Random r = new System.Random();
        int randomIndex = 0;
        while (inputList.Count > 0)
        {
            randomIndex = r.Next(0, inputList.Count); //Choose a random object in the list
            randomList.Add(inputList[randomIndex]); //add it to the new, random list
            inputList.RemoveAt(randomIndex); //remove to avoid duplicates
        }

        return randomList; //return the new random list
    }
}
