﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using EdiFabric.Framework.Envelopes.X12;
using EdiFabric.Framework.Messages;
using EdiFabric.Sdk.ObjectToEdi.CustomClasses.X12;
using EdiFabric.Sdk.ObjectToEdi.EdiMaps.Helpers;

namespace EdiFabric.Sdk.ObjectToEdi.ConsoleApplication.Helpers
{
    class X12Helper
    {
        private static Message Generate810MessageWithCodeMap(Custom810 custom810)
        {
            // Map the custom EDI into ediFabric EDI (the instance of the class that represents the transaction set & version)
            // This uses a code map (I use Automapper here, but can be any code\mapper map)
            var ediFabric810 = custom810.Map();

            // Create ediFabric message (this serializes the instance and passes the xml + context (transaction set, version, format))
            var msg = new Message(ediFabric810);

            // Validate to ensure it's a valid EDI message
            var brokenRules = msg.Validate().ToList();

            if (brokenRules.Any())
            {
                // Do something if the message is invalid
                foreach (var br in brokenRules)
                    Debug.WriteLine(br);
            }

            return msg;
        }

        private static S_GS CreateGs(Message message)
        {
            return new S_GS
            {
                // Functional ID Code
                D_479_1 = "IN",
                // Application Senders Code
                D_142_2 = "SENDERID",
                // Application Receivers Code
                D_124_3 = "PARTNERID",
                // Date
                D_29_4 = DateTime.Now.Date.ToString("YYMMdd"),
                // Time
                D_30_5 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                // Group Control Number
                // Must be unique to both partners for this interchange
                D_28_6 = "111111111",
                // Responsible Agency Code
                D_455_7 = "X",
                // Version/Release/Industry id code
                D_480_8 = message.Context.Version
            };
        }

        private static S_GE CreateGe()
        {
            return new S_GE
            {
                // Number of Transaction Sets Included
                D_97_1 = "1",
                // Group Control Number
                // Must be the same as GS Group Control Number
                D_28_2 = "111111111"
            };
        }

        private static S_ISA CreateIsa()
        {
            return new S_ISA
            {
                // Authorization Information Qualifier
                D_744_1 = "00",
                // Authorization Information
                D_745_2 = "          ",
                // Security Information Qualifier
                D_746_3 = "00",
                // Security Information
                D_747_4 = "          ",
                // Interchange ID Qualifier
                D_704_5 = "01",
                // Interchange Sender
                D_705_6 = "SENDERID",
                // Interchange ID Qualifier
                D_704_7 = "02",
                // Interchange Receiver
                D_706_8 = "PARTNERID",
                // Date
                D_373_9 = DateTime.Now.Date.ToString("YYMMdd"),
                // Time
                D_337_10 = DateTime.Now.TimeOfDay.ToString("hhmm"),
                // Standard identifier
                D_726_11 = "U",
                // Interchange Version ID
                // This is the ISA version and not the transaction sets versions
                D_703_12 = "00204",
                // Interchange Control Number
                D_709_13 = "111111111",
                // Acknowledgment Requested (0 or 1)
                D_749_14 = "1",
                // Test Indicator
                D_748_15 = "T",
                // Component separator
                D_701_16 = ">"
            };
        }

        private static S_IEA CreateIea(int groupsCount)
        {
            return new S_IEA
            {
                // Number of Included Functional groups
                D_405_1 = groupsCount.ToString(),
                // Interchange Control Reference
                // Must be the same as the Interchange Control Number in ISA
                D_709_2 = "111111111"
            };
        }

        private static Group CreateGroup(Message message)
        {
            return new Group
            {
                Ge = CreateGe(),
                Gs = CreateGs(message),
                Messages = new List<Message> {message}
            };
        }

        internal static Interchange CreateInterchange(Custom810 custom810)
        {
            var message = Generate810MessageWithCodeMap(custom810);
            var interchange = new Interchange
            {
                Isa = CreateIsa(),
                Groups = new List<Group> {CreateGroup(message)}
            };

            interchange.Iea = CreateIea(interchange.Groups.Count);

            return interchange;
        }

        internal static Custom810 CreateSampleCustom810()
        {
            // Create a sample instance of a custom type
            // This will be your message(s) that have to be converted into EDI
            const string sampleEdi = "EdiFabric.Sdk.ObjectToEdi.ConsoleApplication.TestFiles.CustomX12.xml";
            var xEl = XElement.Load(Assembly.GetExecutingAssembly().GetManifestResourceStream(sampleEdi));
            return XslHelper.Deserialize<Custom810>(xEl, "customx12");
        }
    }
}
