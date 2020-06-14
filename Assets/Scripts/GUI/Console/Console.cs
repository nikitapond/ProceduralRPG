using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
public class Console : MonoBehaviour
{
    public static Console Instance;
    private static Dictionary<string, ConsoleCommand> ConsoleCommands;

    public bool Active;
    public GameObject ConsolePlane;
    //public Canvas ConsoleCanvas;
    public ScrollRect ScrollRect;
    public Text ConsoleText;
    public Text InputText;
    public InputField ConsoleInput;

    private void Awake()
    {
        if (Instance == null)
        {
            ConsoleCommands = new Dictionary<string, ConsoleCommand>();
            Instance = this;
            Active = false;
            ConsolePlane.gameObject.SetActive(Active);
            AddAllCommands();
        }
    }

    private void Start()
    {
        ConsolePlane.gameObject.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            Debug.Log(Active);
            Active = !Active;
            ConsolePlane.gameObject.SetActive(Active);
            GameManager.SetPause(Active);
        }

        if(Active && Input.GetKeyDown(KeyCode.Return))
        {
            ConsoleEnter();
        }
            
    }

    private void ConsoleEnter()
    {
        Debug.Log(InputText.text);
        AddMessageToConsole(InputText.text);
        ParseInput(InputText.text);
        InputText.text = "";
    }

    private void ParseInput(string input)
    {
        string[] _input = input.Split(null);
        if (_input.Length == 0 || _input == null)
            AddMessageToConsole("Command not valid");

        if(_input.Length == 1)
        {
            if(ConsoleCommands.TryGetValue(_input[0], out ConsoleCommand command))
            {
                string res = command.RunCommand(null);
                if (res != null)
                    AddMessageToConsole(res);
            }
        }
        else
        {
            if (ConsoleCommands.TryGetValue(_input[0], out ConsoleCommand command))
            {
                string res = command.RunCommand(_input.Skip(1).ToArray());
                if (res != null)
                    AddMessageToConsole(res);
            }
        }

    }

    public void AddMessageToConsole(string msg)
    {
        ConsoleText.text += msg + "\n";
        ScrollRect.verticalNormalizedPosition = 0;
    }

    public static void AddCommandToConsole(string name, ConsoleCommand command)
    {
        if (ConsoleCommands == null)
            ConsoleCommands = new Dictionary<string, ConsoleCommand>();

        if (!ConsoleCommands.ContainsKey(name))
            ConsoleCommands.Add(name, command);
    }


    public static void AddAllCommands()
    {
        ConsoleCommands.Add("list", new ConsoleCommandList());
        ConsoleCommands.Add("tp", new ConsoleCommandTP());
        ConsoleCommands.Add("settledata", new ConsoleCommandSettlementData());
    }

}

public abstract class ConsoleCommand
{


    public abstract string RunCommand(string[] args);

}
