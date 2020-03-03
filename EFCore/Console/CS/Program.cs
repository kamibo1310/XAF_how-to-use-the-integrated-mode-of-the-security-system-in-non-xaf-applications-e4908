﻿using DevExpress.ExpressApp;
using DevExpress.ExpressApp.EFCore;
using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.Persistent.BaseImpl.EF.PermissionPolicy;
using System;
using System.Configuration;
using System.IO;
using System.Text;
using Microsoft.EntityFrameworkCore;
using BusinessObjectsLibrary.EFCore.NetCore.BusinessObjects;
using DevExpress.ExpressApp.DC;
using DevExpress.EntityFrameworkCore.Security;

namespace ConsoleApplication {
    class Program {
        static void Main() {
            AuthenticationStandard authentication = new AuthenticationStandard();
            SecurityStrategyComplex security = new SecurityStrategyComplex(typeof(PermissionPolicyUser), typeof(PermissionPolicyRole), authentication);

            string connectionString = ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString;
            SecuredEFCoreObjectSpaceProvider securedObjectSpaceProvider = new SecuredEFCoreObjectSpaceProvider(security, typeof(ConsoleDbContext), XafTypesInfo.Instance, connectionString,
                (builder, connectionString) =>
                 builder.UseSqlServer(connectionString));

            RegisterEntities();

            PasswordCryptographer.EnableRfc2898 = true;
            PasswordCryptographer.SupportLegacySha512 = false;

            string userName = "User";
            string password = string.Empty;
            authentication.SetLogonParameters(new AuthenticationStandardLogonParameters(userName, password));
            IObjectSpace loginObjectSpace = securedObjectSpaceProvider.CreateNonsecuredObjectSpace();
            security.Logon(loginObjectSpace);

            using(StreamWriter file = new StreamWriter("result.txt", false)) {
                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.Append($"{userName} is logged on.\n");
                stringBuilder.Append("List of the 'Person' objects:\n");
                using(IObjectSpace securedObjectSpace = securedObjectSpaceProvider.CreateObjectSpace()) {
                    foreach(Person person in securedObjectSpace.GetObjects<Person>()) {
                        stringBuilder.Append("=========================================\n");
                        stringBuilder.Append($"Full name: {person.FullName}\n");
                        if(security.CanRead(person, nameof(person.Email))) {
                            stringBuilder.Append($"Email: {person.Email}\n");
                        } else {
                            stringBuilder.Append("Email: [Protected content]\n");
                        }
                    }
                }
                file.Write(stringBuilder);
            }
            Console.WriteLine(string.Format(@"The result.txt file has been created in the {0} directory.", Environment.CurrentDirectory));
            Console.WriteLine("Press any key to close.");
            Console.ReadLine();
        }
        private static void RegisterEntities() {
            XafTypesInfo.Instance.RegisterEntity(typeof(Person));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyUser));
            XafTypesInfo.Instance.RegisterEntity(typeof(PermissionPolicyRole));
        }
    }
}