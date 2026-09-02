//
// Copyright (c) 2010-2026 Antmicro
//
// This file is licensed under the MIT License.
// Full license text is available in 'licenses/MIT.txt'.
//
using System;
using System.Runtime.InteropServices;
using System.Text;

using Antmicro.Renode.Exceptions;
using Antmicro.Renode.Logging;

namespace Antmicro.Renode.Network.ExternalControl;

public class CustomCommand(ExternalControlSocket parent) : BaseCommand(parent)
{
    public override MessagePayload Invoke(MessagePayload payload)
    {
        parent.DebugLog("In CustomCommand Invoke method");
        var data = payload.Data;
        if(data.Length != 5)
        {
            return MessagePayload.Error(Identifier, $"Expected 5 bytes payload");
        }
        var command = (CustomCommandCommand)data[0];
        parent.DebugLog("Received {0} CustomCommand command", command);

        switch(command)
        {
        case CustomCommandCommand.RegisterCallbacks:
            var ed = (int) BitConverter.ToInt32(data[1..]);
            parent.DebugLog("Attaching CustomCommand callback");
            lock(commandLock)
            {
                if(CustomCommandHandlerId.HasValue)
                {
                    parent.WarningLog("Overwriting CustomCommand handler ID {0} with {1}", CustomCommandHandlerId, ed);
                }
                CustomCommandHandlerId = ed;
            }
            break;
        default:
            return MessagePayload.Error(Identifier, "Unexpected command format");
        }
        return MessagePayload.Success(Identifier);
    }

    public String Send(String command, ulong timestamp)
    {
        MessagePayload response;
        lock(commandLock)
        {
            if(!CustomCommandHandlerId.HasValue)
            {
                throw new RecoverableException("Command callback is not registered");
            }
            var eventHeader = new EventHeader { TimestampNanoseconds = timestamp};
            response = parent.SendRequest(MessagePayload.Event(Command.CustomCommand, 0, eventHeader, Encoding.UTF8.GetBytes(command)));
        }

        var commandSuccesful = response.LogOnError(Identifier, parent);
        if(!commandSuccesful)
        {
            throw new RecoverableException("Command failed");
        }

        try
        {
            var decoded_response = Encoding.UTF8.GetString(response.Data);

            return decoded_response;
        }
        catch(ArgumentException e)
        {
            var message = e.Message;
            throw new RecoverableException("Cannot decode an error response for command {Identifier}:'{command}' due to '{message}' (raw data: {response.Data})");
        }
    }

    public override Command Identifier => Command.CustomCommand;

    public int? CustomCommandHandlerId = null;

    private readonly Object commandLock = new Object();

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct EventHeader
    {
        public ulong TimestampNanoseconds;
    }

    public enum CustomCommandCommand : byte
    {
        RegisterCallbacks = 0,
    }
}
