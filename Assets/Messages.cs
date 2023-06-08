using System;
using UnityEngine;
using Unity.Networking.Transport;

public enum MessageCode : byte
{
    ConnectReq = 10,
    ConnectAck = 11,
    ConnectFail = 12,
    ConnectOk = 13,

    UpdatePlayersList = 20,
    UpdatePlayerPosition = 21,
    UpdatePlayerHP = 22,
    SetEnemies = 23,
    AddBullet = 24,
    RemBullet = 25,

    CommandMove = 30,
    CommandLook = 31,
    CommandShoot = 32,
}

public abstract class Message
{
    public MessageCode Code;
}

#region Connection
public class ConnectReq : Message
{
    public string Name;
    public ConnectReq()
    {
        Code = MessageCode.ConnectReq;
    }
}

public class ConnectAck : Message
{
    public byte ColorCode;
    public ConnectAck()
    {
        Code = MessageCode.ConnectAck;
    }
}

public class ConnectFail : Message
{
    public enum FailReason { ServerOverflow = 0, NameExists = 1, GameIsAlreadyStarted = 2, Unexpected = 3 }
    public FailReason Reason;
    public ConnectFail()
    {
        Code = MessageCode.ConnectFail;
    }
}

public class ConnectOk : Message
{
    public ConnectOk()
    {
        Code = MessageCode.ConnectOk;
    }
}
#endregion

#region ServerBehaviour
public class UpdatePlayersList : Message
{
    public byte amount;
    public Tuple<string, byte>[] playerDatas;
    public UpdatePlayersList()
    {
        Code = MessageCode.UpdatePlayersList;
    }
}

public class UpdatePlayerPosition : Message
{
    public byte playerCode;
    public Vector2 position;
    public UpdatePlayerPosition()
    {
        Code = MessageCode.UpdatePlayerPosition;
    }
}

public class UpdatePlayerHP : Message
{
    public byte colorCode;
    public byte HP;
    public UpdatePlayerHP()
    {
        Code = MessageCode.UpdatePlayerHP;
    }
}

public class SetEnemies : Message
{
    public byte amount;
    public Vector2[] enemyPositions;
    public SetEnemies()
    {
        Code = MessageCode.SetEnemies;
    }
}

public class AddBullet : Message
{
    public ushort bulletID;
    public Vector2 position;
    public float rotationZ;
    public AddBullet()
    {
        Code = MessageCode.AddBullet;
    }
}

public class RemBullet : Message
{
    public ushort bulletID;
    public RemBullet()
    {
        Code = MessageCode.RemBullet;
    }
}
#endregion

#region Commands
public class CommandMove : Message
{
    public Vector2 direction;
    public CommandMove()
    {
        Code = MessageCode.CommandMove;
    }
}

public class CommandLook : Message
{
    public float rotationZ;
    public byte playerCode;
    public CommandLook()
    {
        Code = MessageCode.CommandLook;
    }
}

public class CommandShoot : Message
{
    public CommandShoot()
    {
        Code = MessageCode.CommandShoot;
    }
}
#endregion

public static class MessageExtensions
{
    public static Message RecieveSSPMessage(this DataStreamReader stream)
    {
        MessageCode code = (MessageCode)stream.ReadByte();

        #region Connection
        if (code == MessageCode.ConnectReq)
            return new ConnectReq() { Name = stream.ReadFixedString32().ToString() };

        else if (code == MessageCode.ConnectAck)
            return new ConnectAck() { ColorCode = stream.ReadByte() };

        else if (code == MessageCode.ConnectFail)
            return new ConnectFail() { Reason = (ConnectFail.FailReason)stream.ReadByte() };

        else if (code == MessageCode.ConnectOk)
            return new ConnectOk();
        #endregion

        #region ServerBehaviour
        else if (code == MessageCode.UpdatePlayersList)
        {
            UpdatePlayersList message = new();

            message.amount = stream.ReadByte();
            message.playerDatas = new Tuple<string, byte>[message.amount];

            for (int i = 0; i < message.amount; i++)
                message.playerDatas[i] = new Tuple<string, byte>(stream.ReadFixedString32().ToString(), stream.ReadByte());

            return message;
        }

        else if (code == MessageCode.UpdatePlayerPosition)
            return new UpdatePlayerPosition() { playerCode = stream.ReadByte(), position = new Vector2(stream.ReadFloat(), stream.ReadFloat())};

        else if (code == MessageCode.UpdatePlayerHP)
            return new UpdatePlayerHP() { colorCode = stream.ReadByte(), HP = stream.ReadByte() };

        else if (code == MessageCode.SetEnemies)
        {
            SetEnemies message = new();

            message.amount = stream.ReadByte();
            message.enemyPositions = new Vector2[message.amount];

            for (int i = 0; i < message.amount; i++)
                message.enemyPositions[i] = new Vector2(stream.ReadFloat(), stream.ReadFloat());

            return message;
        }

        else if (code == MessageCode.AddBullet)
            return new AddBullet() { bulletID = stream.ReadUShort(), position = new Vector2(stream.ReadFloat(), stream.ReadFloat()), rotationZ = stream.ReadFloat() };

        else if (code == MessageCode.RemBullet)
            return new RemBullet() { bulletID = stream.ReadUShort() };
        #endregion

        #region Commands
        else if (code == MessageCode.CommandMove)
        {
            CommandMove message = new CommandMove();
            byte dirCode = stream.ReadByte();
            message.direction = new Vector2(dirCode % 10 - 1, dirCode / 10 - 1).normalized;
            return message;
        }

        else if (code == MessageCode.CommandLook)
            return new CommandLook() { playerCode = stream.ReadByte(), rotationZ = stream.ReadFloat() };

        else if (code == MessageCode.CommandShoot)
            return new CommandShoot();
        #endregion

        throw new Exception("Unknown message type");
    }

    public static void SendSSPMessage(this NetworkDriver m_Driver, NetworkConnection m_Connection, Message message)
    {
        m_Driver.BeginSend(m_Connection, out var writer);
        writer.WriteByte((byte)message.Code);

        #region Connection
        if (message is ConnectReq)
        {
            ConnectReq msg = (ConnectReq)message;
            writer.WriteFixedString32(msg.Name);
        }
        else if (message is ConnectAck)
        {
            ConnectAck msg = (ConnectAck)message;
            writer.WriteByte(msg.ColorCode);
        }
        else if (message is ConnectFail)
        {
            ConnectFail msg = (ConnectFail)message;
            writer.WriteByte((byte)msg.Reason);
        }
        else if (message is ConnectOk)
        { }
        #endregion

        #region ServerBehaviour
        else if (message is UpdatePlayersList)
        {
            UpdatePlayersList msg = (UpdatePlayersList)message;
            writer.WriteByte(msg.amount);
            foreach(Tuple<string, byte> data in msg.playerDatas)
            {
                writer.WriteFixedString32(data.Item1);
                writer.WriteByte(data.Item2);
            }
        }
        else if (message is UpdatePlayerPosition)
        {
            UpdatePlayerPosition msg = (UpdatePlayerPosition)message;
            writer.WriteByte(msg.playerCode);
            writer.WriteFloat(msg.position.x);
            writer.WriteFloat(msg.position.y);
        }
        else if (message is UpdatePlayerHP)
        {
            UpdatePlayerHP msg = (UpdatePlayerHP)message;
            writer.WriteByte(msg.colorCode);
            writer.WriteByte(msg.HP);
        }
        else if (message is SetEnemies)
        {
            SetEnemies msg = (SetEnemies)message;
            writer.WriteByte(msg.amount);
            foreach(Vector2 position in msg.enemyPositions)
            {
                writer.WriteFloat(position.x);
                writer.WriteFloat(position.y);
            }
        }
        else if (message is AddBullet)
        {
            AddBullet msg = (AddBullet)message;
            writer.WriteUShort(msg.bulletID);
            writer.WriteFloat(msg.position.x);
            writer.WriteFloat(msg.position.y);
            writer.WriteFloat(msg.rotationZ);
        }
        else if (message is RemBullet)
        {
            RemBullet msg = (RemBullet)message;
            writer.WriteUShort(msg.bulletID);
        }
        #endregion

        #region Commands
        else if (message is CommandMove)
        {
            CommandMove msg = (CommandMove)message;
            byte dir = (byte)(msg.direction.x + (byte)msg.direction.y * 10 + 11);
            writer.WriteByte(dir);
        }
        else if (message is CommandLook)
        {
            CommandLook msg = (CommandLook)message;
            writer.WriteByte(msg.playerCode);
            writer.WriteFloat(msg.rotationZ);
        }
        else if (message is CommandShoot)
        { }
        #endregion

        m_Driver.EndSend(writer);
    }
}