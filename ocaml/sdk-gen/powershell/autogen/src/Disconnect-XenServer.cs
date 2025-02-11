/*
 * Copyright (c) Citrix Systems, Inc.
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions
 * are met:
 *
 *   1) Redistributions of source code must retain the above copyright
 *      notice, this list of conditions and the following disclaimer.
 *
 *   2) Redistributions in binary form must reproduce the above
 *      copyright notice, this list of conditions and the following
 *      disclaimer in the documentation and/or other materials
 *      provided with the distribution.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS
 * FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE
 * COPYRIGHT HOLDER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT,
 * INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR
 * SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION)
 * HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT,
 * STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Linq;
using System.Management.Automation;

using XenAPI;

namespace Citrix.XenServer.Commands
{
    [Cmdlet("Disconnect", "XenServer", DefaultParameterSetName = "XenObject")]
    public class DisconnectXenServerCommand : PSCmdlet
    {
        [Parameter(ParameterSetName = "XenObject", ValueFromPipeline = true, Position = 0)]
        public Session Session { get; set; }

        [Parameter(ParameterSetName = "Ref", ValueFromPipelineByPropertyName = true, Position = 0)]
        [Alias("opaque_ref")]
        public XenRef<Session> Ref { get; set; }

        protected override void ProcessRecord()
        {
            var sessions = CommonCmdletFunctions.GetAllSessions(this);

            if (sessions.Count == 0)
            {
                ThrowTerminatingError(new ErrorRecord(
                    new Exception("Could not find open sessions to any XenServers."),
                    "",
                    ErrorCategory.InvalidArgument, null));
            }

            Session session = null;

            if (Session == null && Ref == null)
            {
                if (sessions.Count == 1)
                {
                    session = sessions.Values.FirstOrDefault();
                }
                else
                {
                    Session defaultSession = CommonCmdletFunctions.GetDefaultXenSession(this);

                    if (sessions.ContainsValue(defaultSession))
                        session = defaultSession;

                    if (session == null)
                        ThrowTerminatingError(new ErrorRecord(
                            new Exception("A default XenServer session has not been set."),
                            "",
                            ErrorCategory.InvalidArgument, null));
                }
            }
            else
            {
                if (Session != null && sessions.ContainsKey(Session.opaque_ref))
                    session = Session;
                else if (Ref != null && sessions.ContainsKey(Ref.opaque_ref))
                    session = sessions[Ref.opaque_ref];

                if (session == null)
                    ThrowTerminatingError(new ErrorRecord(
                        new Exception("Could not locate the specified session in the open XenServer sessions."),
                        "",
                        ErrorCategory.InvalidArgument,
                        Session != null ? Session.opaque_ref : Ref.opaque_ref));
            }

            if (session == null)
                return;

            //store the session's opaque_ref as logging out sets it to null
            var sessionRef = session.opaque_ref;
            try
            {
                session.logout();
            }
            finally
            {
                WriteVerbose("Removing session from Citrix.XenServer.Sessions variable.");
                sessions.Remove(sessionRef);
            }
        }
    }
}
