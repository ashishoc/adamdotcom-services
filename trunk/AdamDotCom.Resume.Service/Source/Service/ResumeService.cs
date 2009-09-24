﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using AdamDotCom.Resume.Service.Extensions;
using AdamDotCom.Resume.Service.Utilities;

namespace AdamDotCom.Resume.Service
{
    public class ResumeService : IResume
    {
        public Resume ResumeXml(string firstnameLastname)
        {
            return Resume(firstnameLastname);
        }

        public Resume ResumeJson(string firstnameLastname)
        {
            return Resume(firstnameLastname);
        }

        private static Resume Resume(string firstnameLastname)
        {
            AssertValidInput(firstnameLastname, "firstname-lastname");

            firstnameLastname = Scrub(firstnameLastname);

            if (ServiceCache.IsInCache(firstnameLastname))
            {
                return (Resume)ServiceCache.GetFromCache(firstnameLastname);
            }

            var linkedInEmailAddress = ConfigurationManager.AppSettings["LinkedInEmailAddress"];
            var linkedInPassword = ConfigurationManager.AppSettings["LinkedInPassword"];

            var resumeSniffer = new LinkedInResumeSniffer(linkedInEmailAddress, linkedInPassword, firstnameLastname);

            Resume resume = null;
            try
            {
                resume = resumeSniffer.GetResume();
            }
            catch (Exception ex)
            {
                HandleErrors(linkedInEmailAddress);
            }

            HandleErrors(resumeSniffer.Errors);           

            return resume.AddToCache(firstnameLastname);
        }

        private static string Scrub(string username)
        {
            return username.Replace("%20", " ").Replace("-", " ");
        }

        private static void AssertValidInput(string inputValue, string inputName)
        {
            inputName = (string.IsNullOrEmpty(inputName) ? "Unknown" : inputName);

            if (string.IsNullOrEmpty(inputValue) || inputValue.Equals("null", StringComparison.CurrentCultureIgnoreCase))
            {
                throw new RestException(HttpStatusCode.BadRequest,
                                        new List<KeyValuePair<string, string>> { new KeyValuePair<string, string>(inputName, string.Format("{0} is not a valid value.", inputValue)) },
                                        (int)ErrorCode.InternalError);
            }
        }

        private static void HandleErrors(List<KeyValuePair<string, string>> errors)
        {
            
            if (errors != null && errors.Count != 0)
            {
                var criticalErrors = errors.Where(e => e.Key.Contains("Critical")).ToList();
                if (criticalErrors.Count != 0)
                {
                    throw new RestException(HttpStatusCode.BadRequest, criticalErrors, (int)ErrorCode.InternalError);
                }
            }
        }

        private static void HandleErrors(string linkedInEmailAddress)
        {
            throw new RestException(HttpStatusCode.BadRequest,
                                    new List<KeyValuePair<string, string>>
                                        {
                                            new KeyValuePair<string, string>("LinkedInResumeSniffer",
                                                                             string.Format(
                                                                                 "The requested resume could not be retrieved. Ensure that you have added {0} as a LinkedIn contact, alternatively you can download the source code ({1}) and contribute a patch for your resume.",
                                                                                 linkedInEmailAddress, "http://code.google.com/p/adamdotcom-services/source/checkout"))
                                        }, (int)ErrorCode.InternalError);
        }
    }
}