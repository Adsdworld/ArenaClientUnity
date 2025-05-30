using UnityEngine;
using Script.Utils;
using Script.Game.Player;
using Script.Game.Core;
using Script.Network.Message;
using WebSocketSharp;
using System;
using Newtonsoft.Json;


namespace Script.Network.Transport
{
    public class UnityWebSocket : MonoBehaviour
    {
        public static UnityWebSocket Instance { get; private set; }

        private WebSocket _websocket;
        public Boolean production;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start() // Do not add code here, add code OnOpen(), OnMessage(), OnError(), OnClose().
        {
            Log.Info("Initialisation du client WebSocket...");
            
            MessageService.MessageSender = new UnityWebSocketMessageSender();

            _websocket = production? new WebSocket("ws://arenafr.servegame.com:54099") : new WebSocket("ws://localhost:54099");

            _websocket.OnOpen += OnOpen;
            _websocket.OnMessage += OnMessage;
            _websocket.OnError += OnError;
            _websocket.OnClose += OnClose;

            _websocket.ConnectAsync();
        }

        private void OnOpen(object sender, EventArgs e)
        {
            Log.Info("Connexion WebSocket établie.");
            MainThreadDispatcher.Enqueue(() =>
            {
                Message.Message message = new Message.Message();
                message.SetAction(ActionEnum.Login);
                message.SetUuid(UuidManager.GetUuid());
                message.Send();

            });
            //Log.Info("Message de connexion envoyé.");
        }

        private void OnMessage(object sender, MessageEventArgs e)
        {
            Log.Info("Message reçu du serveur : " + e.Data);
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            Log.Failure("Erreur WebSocket : " + e.Message);
        }

        private void OnClose(object sender, CloseEventArgs e)
        {
            Log.Info("Connexion WebSocket fermée. Code: " + e.Code + ", Raison: " + e.Reason);
        }

        public static void SendMessage(Message.Message msg)
        {
            if (Instance == null)
            {
                Log.Warn("Tentative d'envoi alors que le client WebSocket n'est pas initialisé.");
                return;
            }

            if (msg == null)
            {
                Log.Failure("Tentative d'envoi d'un message null.");
                return;
            }

            if (Instance._websocket == null || !Instance._websocket.IsAlive)
            {
                Log.Warn("WebSocket non connecté.");
                return;
            }

            string json = JsonConvert.SerializeObject(msg);

            if (string.IsNullOrEmpty(json))
            {
                Log.Failure("Message JSON null ou vide.");
                return;
            }

            try
            {
                Instance._websocket.Send(json);
                Log.Info("Message envoyé au serveur : " + json);
            }
            catch (Exception e)
            {
                Log.Info("Erreur lors de l'envoi du message : " + json);
                Log.Failure("Erreur : " + e.Message);
            }
        }

        private void OnDestroy()
        {
            if (_websocket != null)
            {
                _websocket.Close();
                _websocket = null;
                Log.Info("WebSocket détruit.");
            }
        }
    }
}
