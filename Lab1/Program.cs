﻿namespace Lab1
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Runtime.Versioning;
    using System.Security.Principal;
    using Registration;

    class Program
    {
        private static readonly Dictionary<Role, Dictionary<Disk, List<Right>>> RolesRights = new()
        {
            {
                Role.Admin, new Dictionary<Disk, List<Right>>
                {
                    {Disk.A, new List<Right> {Right.Read, Right.Write, Right.Execute}},
                    {Disk.B, new List<Right> {Right.Read, Right.Write, Right.Execute}},
                    {Disk.C, new List<Right> {Right.Read, Right.Write, Right.Execute}},
                    {Disk.System, new List<Right> {Right.Read, Right.Write, Right.Execute}},
                    {Disk.None, new List<Right>()}
                }
            },
            {
                Role.User, new Dictionary<Disk, List<Right>>
                {
                    {Disk.A, new List<Right> {Right.Read}},
                    {Disk.B, new List<Right>()},
                    {Disk.C, new List<Right> {Right.Read, Right.Write}},
                    {Disk.System, new List<Right>()},
                    {Disk.None, new List<Right>()}
                }
            }
        };

        private static readonly Dictionary<Command, Right> CommandsRights = new()
        {
            {Command.Ls, Right.Read},
            {Command.Cat, Right.Read},
            {Command.Touch, Right.Write},
            {Command.Rm, Right.Write},
            {Command.Execute, Right.Execute}
        };

        [SupportedOSPlatform("windows")]
        private static void Main(string[] args)
        {
            var inputValidator = new InputValidator();
            var inputCommand = args[0];

            if (inputValidator.ValidateCommand(inputCommand, out var command))
            {
                HandleInput(command, args);
                return;
            }

            Console.WriteLine($"Unknown command: {inputCommand}");
        }

        [SupportedOSPlatform("windows")]
        private static void HandleInput(Command command, string[] args)
        {
            var role = GetRole();
            var path = args[1];
            
            if (CommandsRights.TryGetValue(command, out var right))
            {
                var disk = GetDisk(path);
                    
                var isRoleGrantedAccess = IsRoleGrantedAccess(role, disk, right);
                if (!isRoleGrantedAccess)
                {
                    Console.WriteLine("You don't have permissions to perform this operation");
                    return;
                }
                
                if (args.Length == 3) PerformOperation(command, path, args[2]);
                else PerformOperation(command, path);
                return;
            }
            
            if (command == Command.Register)
            {
                var isRoleGrantedAccess = IsRoleGrantedAccess(role, Disk.System, Right.Write);
                if (!isRoleGrantedAccess)
                {
                    Console.WriteLine("You don't have permissions to perform this operation");
                    return;
                }

                var userRegistrar = new UserRegistrar();
                userRegistrar.CreateUser(args[1], args[2]);
                return;
            }

            Console.WriteLine("Command has not specified rights");
        }

        [SupportedOSPlatform("windows")]
        private static Role GetRole()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator) ? Role.Admin : Role.User;
        }

        private static bool IsRoleGrantedAccess(Role role, Disk disk, Right right)
        {
            return RolesRights[role][disk].Contains(right);
        }

        private static Disk GetDisk(string path)
        {
            var absPath = Path.GetFullPath(path);
            var currentPath = AppDomain.CurrentDomain.BaseDirectory;
            try
            {
                var disk = absPath[$"{currentPath}Storage\\".Length..][0];
                switch (disk)
                {
                    case 'A':
                        return Disk.A;
                    case 'B':
                        return Disk.B;
                    case 'C':
                        return Disk.C;
                }
            }
            catch
            {
                return Disk.None;
            }

            return Disk.None;
        }

        private static void PerformOperation(Command command, string path, string additionalArg = null)
        {
            switch (command)
            {
                case Command.Ls:
                    PerformLs(path);
                    break;
                case Command.Cat:
                    PerformCat(path);
                    break;
                case Command.Touch:
                    PerformTouch(path, additionalArg);
                    break;
                case Command.Rm:
                    PerformRm(path);
                    break;
                case Command.Execute:
                default:
                    PerformExecute(path);
                    break;
            }
        }

        private static void PerformLs(string path)
        {
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path)
                    .Select(filePath => filePath[(path.Length + 1)..]);
                Console.WriteLine(string.Join('\n', files));
            }
            else
            {
                Console.WriteLine($"{path} is not a valid directory.");
            }
        }

        private static void PerformCat(string path)
        {
            if (File.Exists(path))
            {
                var file = File.ReadAllText(path);
                Console.WriteLine(file);
            }
            else
            {
                Console.WriteLine($"{path} is not a valid file.");
            }
        }

        private static void PerformTouch(string path, string filename)
        {
            if (Directory.Exists(path))
            {
                File.Create($"{path}/{filename}");
            }
            else
            {
                Console.WriteLine($"{path} is not a valid directory.");
            }
        }

        private static void PerformRm(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
            else
            {
                Console.WriteLine($"{path} is not a valid directory.");
            }
        }

        private static void PerformExecute(string path)
        {
            if (File.Exists(path))
            {
                Process.Start(path);
            }
            else
            {
                Console.WriteLine($"{path} is not a valid file.");
            }
        }
    }
}