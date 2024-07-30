using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

using com.vchatcloud.unity.callback.channelCallback;
using com.vchatcloud.unity.callback.joinChannelCallback;
using com.vchatcloud.unity.constant.socketSet;
using com.vchatcloud.unity.constant.stringSet;
using com.vchatcloud.unity.handlers.messageHandler;
using com.vchatcloud.unity.manager.channelOptions;
using com.vchatcloud.unity.socketManager;
using com.vchatcloud.unity.vChatCloud;
using com.vchatcloud.unity.vChatCloud.com.vchatcloud.unity.vChatCloudException;
using static com.vchatcloud.unity.vChatCloud.VChatCloud;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Threading.Tasks; 
using System.Collections.Generic;
using PimDeWitte.UnityMainThreadDispatcher;

public class UnityVChat : MonoBehaviour
{

    [SerializeField]
    private TextField chatInput;

    [SerializeField]
    private Button sendButton;

    [SerializeField]
    private ScrollView chatScrollView;

    public string deviceUuid;
    private string room_id;
    private string nick_name;
    private string profile;
    private static JObject userInfo = null;
    private static Channel channel = null;
    private ChannelOptions options;

 
    private async void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        chatInput = root.Q<TextField>("chatInput");
        sendButton = root.Q<Button>("sendButton");
        chatScrollView = root.Q<ScrollView>("chatScrollView");

        sendButton.clicked += OnSendButtonClicked;

        await ConnectAndJoinRoomAsync();
    }

    private void OnSendButtonClicked()
    {
        string message = chatInput.value;
        if (!string.IsNullOrEmpty(message))
        {
            //AddMessageToChat(message);


            JObject param = new JObject();
            param["message"] = message;
            param["mimeType"] = "text";
            //param["messageType"] = messageType;
            if (userInfo != null)
            {
                param["userInfo"] = userInfo;
            }
            Console.WriteLine(param.ToString());


            sendMsg(param, new ChannelCallbackImp((o, e) => {
                if (e == null)
                {
                    // 성공 시 처리 로직
                    Console.WriteLine("ChannelCallbackImp Message sent successfully. {0}", o);
                    UnityEngine.Debug.Log("ChannelCallbackImp Message sent successfully. " + o);
                }
                else
                {
                    // 예외 처리 로직
                    Console.WriteLine($"Error: {e}");
                    UnityEngine.Debug.LogError($"Error: {e}");
                }
            }));

            chatInput.value = string.Empty;
        }
    }

    public void AddMessageToChat(string message)
    {
        var messageLabel = new Label(message);
        chatScrollView.Add(messageLabel);
        chatScrollView.ScrollTo(messageLabel);
    }

    public void sendMsg(JObject param, ChannelCallback channelCallback)
    {
        //ChannelCallback channelCallback = new ChannelCallbackImp();

        Console.WriteLine("sendMsg channel {0}  ", channel);
        Console.WriteLine("sendMsg param {0}   ", param);
        Console.WriteLine("sendMsg channelCallback {0} ", channelCallback);
        channel.SendMessage(param, channelCallback);
    }

    private async Task ConnectAndJoinRoomAsync()
    {

        // 방 접속을 위한 값 생성 
        room_id = "NpWXoXqzSG-ofk6E5p1xA-20240716140303";
        nick_name = "U-User";
        profile = DateTime.Now.ToString("HHmmss");

        Guid originalGuid = Guid.NewGuid();
        deviceUuid = originalGuid.ToString("D").Substring(0, 8);

        userInfo = new JObject();
        userInfo["profile"] = profile; // TODO : 원래는 1부터 증가시키는 값이지만, 프로그램내에 로컬 저장 기능을 만들어야해서 일단 시스템타임으로 ..

        options = new ChannelOptions();

        options.SetChannelKey(room_id).SetClientKey(deviceUuid).SetNickName(nick_name).SetUserInfo(userInfo);

        // 채팅서버 접속 

        //SocketManager.Instance.OnConnectionOpened += () => Console.WriteLine("접속성공");

        SocketManager.Instance.OnConnectionOpened += () => ReceiveOpenEventAsync().GetAwaiter();

        //SocketManager.Instance.OnConnectionFailed += ex => AddMessageToChat("접속실패 " + ex.Message);
        SocketManager.Instance.OnConnectionFailed += ex => UnityEngine.Debug.Log("접속실패 " + ex.Message);

        //SocketManager.Instance.OnConnectionClosed += () => AddMessageToChat("접속종료");
        SocketManager.Instance.OnConnectionClosed += () => UnityEngine.Debug.Log("접속종료");

        //SocketManager.Instance.OnMessageReceived += message =>  textBox1.Text += Environment.NewLine + message;
        //SocketManager.Instance.OnMessageReceived += message => Console.WriteLine(message);

        SocketManager.Instance.OnMessageReceived += async message => await ReceiveJoinMessagesAsync(message);
        //SocketManager.Instance.OnMessageReceived += message => UnityEngine.Debug.Log("메시지>>" + message);
        //SocketManager.Instance.OnMessageReceived += message => AddMessageToChat("메시지>> " + message);

        VChatCloud.GetInstance().SetSocketStatus(SocketSet.CLOSED);

        try
        {
            SocketManager.Instance.Connect(StringSet.SERVER);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.Log(ex.ToString());                                                                                                            
        }                                                                                                    


        //await ReceiveMessagesAsync();

    }




    private async Task ReceiveOpenEventAsync()
    {

 
        Console.WriteLine("채팅방 접속");
        UnityEngine.Debug.Log("채팅방 접속");

        if (channel == null)
        {
            try
            {
                Console.WriteLine("options {0} ", options);

                Console.WriteLine("Channel 객체 생성");
                UnityEngine.Debug.Log("Channel 객체 생성");
                channel = VChatCloud.GetInstance().JoinChannel(options, new JoinChannelCallbackImpl((history, e) =>
                {
                    int historySize = history.Count - 1;
                    Console.WriteLine("JoinChannel :: historySize {0}", historySize);
                    for (; historySize >= 0; historySize--)
                    {
                        Console.WriteLine("JoinChannel :: history {0}", historySize, history[historySize]);
                        UnityEngine.Debug.Log("############## JoinChannel :: history  " + historySize + " " + history[historySize]);

                    }
                }));

                UnityEngine.Debug.Log("############## Channel SetHandler MessageHandlerEx ");
                channel.SetHandler(new MessageHandlerEx(this));

            }
            catch (Exception ex)
            {
                Console.WriteLine($"{ex.Message}");
                UnityEngine.Debug.Log(ex.Message);
            }

        }
        else
        {
            Console.WriteLine("@@@@@@@@@@ ReceiveOpenEventAsync channel IS NOT NULL ");
            UnityEngine.Debug.Log("@@@@@@@@@@ ReceiveOpenEventAsync channel IS NOT NULL ");
        }


    }


    private async Task ReceiveJoinMessagesAsync(string message)
    {

        Console.WriteLine("++++++ ReceiveJoinMessagesAsync ++++++ " + message);
 

        UnityEngine.Debug.Log("++++++ ReceiveJoinMessagesAsync ++++++ " + message);
 

        var dictionary = JsonConvert.DeserializeObject<Dictionary<string, object>>(message);
        Console.WriteLine("dictionary {0}", dictionary);
        UnityEngine.Debug.Log("dictionary "+ dictionary);

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // AddMessageToChat("메시지>> " + message);
        });

    }

}




public class ChannelCallbackImp : ChannelCallback
{
    private Action<object, object> value;

    public ChannelCallbackImp(Action<object, object> value)
    {
        this.value = value;
    }

    public void callback(object data, VChatCloudException e)
    {
        if (e == null)
        {
            // 성공 시 처리 로직
            Console.WriteLine("ChannelCallbackImp Message sent successfully. {0}", data);
            UnityEngine.Debug.Log("ChannelCallbackImp Message sent successfully.  "+ data);
        }
        else
        {
            // 예외 처리 로직
            Console.WriteLine($"Error: {e.Message}");
            UnityEngine.Debug.Log(e.Message);
        }
    }
}

public class JoinChannelCallbackImpl : JoinChannelCallback
{
    private readonly Action<JArray, VChatCloudException> _callbackAction;

    public JoinChannelCallbackImpl(Action<JArray, VChatCloudException> callbackAction)
    {
        _callbackAction = callbackAction;
    }

    public override void callback(JArray history, VChatCloudException e)
    {
        _callbackAction(history, e);
    }
}

public class MessageHandlerEx : MessageHandler
{

    //private Form1 _form;

    /**
    public MessageHandlerEx(Form1 form)
    {
        _form = form;
    }
    */

    private UnityVChat _unityVChat;

    public MessageHandlerEx(UnityVChat unityVChat)
    {

        UnityEngine.Debug.Log("############## MessageHandlerEx " + unityVChat);

        _unityVChat = unityVChat;
    }

    public MessageHandlerEx()
    {

    }

    public override void OnNotifyMessage(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyMessage {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyMessage " +  data);
        /**
        _form.Invoke((MethodInvoker)delegate
        {
            _form.textBox1.Text += Environment.NewLine + data["nickName"] + " " + data["message"];
        });
        */
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            _unityVChat.AddMessageToChat(data["nickName"] + " " + data["message"]);
        });
    }

    public override void OnNotifyWhisper(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyWhisper {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyWhisper " + data);
    }

    public override void OnPersonalWhisper(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnPersonalWhisper {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnPersonalWhisper " + data);
    }

    public override void OnNotifyNotice(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyNotice {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyNotice " + data);
    }

    public override void OnNotifyCustom(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyCustom {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyCustom " + data);
    }

    public override void OnNotifyJoinUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyJoinUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyJoinUser " + data);
        /**
        _form.Invoke((MethodInvoker)delegate
        {
            _form.textBox1.Text += Environment.NewLine + data["nickName"] + "님이 입장하셨습니다.";
        });
        */

        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            _unityVChat.AddMessageToChat(data["nickName"] + "님이 입장하셨습니다.");
        });
    }

    public override void OnNotifyLeaveUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyLeaveUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyLeaveUser " + data);
        /**
        _form.Invoke((MethodInvoker)delegate
        {
            _form.textBox1.Text += Environment.NewLine + data["nickName"] + "님이 나가셨습니다.";
        });
        */
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            _unityVChat.AddMessageToChat(data["nickName"] + "님이 나가셨습니다.");
        });
    }

    public override void OnPersonalKickUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnPersonalKickUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnPersonalKickUser  " + data);
    }

    public override void OnPersonalMuteUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnPersonalMuteUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnPersonalMuteUser  " + data);
    }

    public override void OnPersonalUnmuteUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnPersonalUnmuteUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnPersonalUnmuteUser  " + data);
    }

    public override void OnPersonalDuplicateUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnPersonalDuplicateUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnPersonalDuplicateUser  " + data);
    }

    public override void OnNotifyKickUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyKickUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyKickUser  " + data);
    }

    public override void OnNotifyUnkickUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyUnkickUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyUnkickUser  " + data);
    }

    public override void OnNotifyMuteUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyMuteUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyMuteUser  " + data);
    }

    public override void OnNotifyUnmuteUser(JObject data)
    {
        Console.WriteLine("MessageHandlerEx :: OnNotifyUnmuteUser {0}", data);
        UnityEngine.Debug.Log("MessageHandlerEx :: OnNotifyUnmuteUser  " + data);
    }
}

public class Message
{
    private string nickName = "";
    private string clientKey = "";
    private string message = "";
    private string mimeType = "";
    private string messageDt = "";
    private string date = "";
    private string roomId = "";
    private string grade = "";
    private string type = "msg";
    private JObject messageType = null;
    private JObject userInfo = null;

    public Message(JObject jsonObject)
    {
        // JSONOBJECT 받아서 한번에 SET 처리 ..

        try
        {
            if (jsonObject.Property("nickName") != null)
            {
                this.nickName = (string)jsonObject["nickName"];
            }
            if (jsonObject.Property("clientKey") != null)
            {
                this.clientKey = (string)jsonObject["clientKey"];
            }
            if (jsonObject.Property("message") != null)
            {
                this.message = (string)jsonObject["message"];
            }
            if (jsonObject.Property("mimeType") != null)
            {
                this.mimeType = (string)jsonObject["mimeType"];
            }
            if (jsonObject.Property("messageDt") != null)
            {
                this.messageDt = (string)jsonObject["messageDt"];
            }
            if (jsonObject.Property("date") != null)
            {
                this.date = (string)jsonObject["date"];
            }
            if (jsonObject.Property("roomId") != null)
            {
                this.roomId = (string)jsonObject["roomId"];
            }
            if (jsonObject.Property("grade") != null)
            {
                this.grade = (string)jsonObject["grade"];
            }
            if (jsonObject.Property("type") != null)
            {
                this.type = (string)jsonObject["type"];
            }
            if (jsonObject.Property("messageType") != null)
            {
                this.messageType = JObject.Parse((string)jsonObject["messageType"]);
            }
            if (jsonObject.Property("userInfo") != null)
            {
                this.userInfo = JObject.Parse((string)jsonObject["userInfo"]);
            }

        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    public JObject getMessageType()
    {
        return messageType;
    }

    public void setMessageType(JObject messageType)
    {
        this.messageType = messageType;
    }

    public JObject getUserInfo()
    {
        return userInfo;
    }

    public void setUserInfo(JObject userInfo)
    {
        this.userInfo = userInfo;
    }

    public string getNickName()
    {
        return nickName;
    }

    public void setNickName(string nickName)
    {
        this.nickName = nickName;
    }

    public string getClientKey()
    {
        return clientKey;
    }

    public void setClientKey(string clientKey)
    {
        this.clientKey = clientKey;
    }

    public string getMessage()
    {
        return message;
    }

    public void setMessage(string message)
    {
        this.message = message;
    }

    public string getMimeType()
    {
        return mimeType;
    }

    public void setMimeType(string mimeType)
    {
        this.mimeType = mimeType;
    }

    public string getMessageDt()
    {
        return messageDt;
    }

    public void setMessageDt(string messageDt)
    {
        this.messageDt = messageDt;
    }

    public string getRoomId()
    {
        return roomId;
    }

    public void setRoomId(string roomId)
    {
        this.roomId = roomId;
    }

    public string getGrade()
    {
        return grade;
    }

    public void setGrade(string grade)
    {
        this.grade = grade;
    }

    public string getDate()
    {
        return date;
    }

    public void setDate(string date)
    {
        this.date = date;
    }

    public string getType()
    {
        return type;
    }

    public void setType(string type)
    {
        this.type = type;
    }
}