﻿using System;

namespace ExampleApp.Tasks
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    internal class TaskAttribute : Attribute
    {
        public TaskAttribute(string command, string description)
        {
            Command = command;
            Description = description;
        }

        public string Description { get; set; }
        public string Command { get; set; }
    }
}