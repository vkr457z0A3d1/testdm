﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Duplicati.Library.Interface;

namespace ZipFileDebugger
{
    class Program
    {
        static void Main(string[] args)
        {
            var argslist = new List<string>(args);
            var opts = Duplicati.Library.Utility.CommandLineParser.ExtractOptions(argslist, null);

            if (argslist == null || argslist.Count == 0)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("{0} <filename.zip>", System.Reflection.Assembly.GetEntryAssembly().Location);
                return;
            }

            var filecount = 0;
            var errorcount = 0;
            foreach (var file in argslist)
            {
                if (!File.Exists(file))
                {
                    Console.WriteLine("File not found: {0}", file);
                    continue;
                }

                if (new FileInfo(file).Length == 0)
                {
                    Console.WriteLine("File is emtpy: {0}", file);
                    continue;
                }

                Console.WriteLine("Opening zip file {0}", file);

                var errors = false;

                try
                {
                    filecount++;
                    using (var filestream = System.IO.File.Open(file, FileMode.Open, FileAccess.Read,
                        FileShare.None))
                    {
                        using (var zr = new Duplicati.Library.Compression.FileArchiveZip(filestream, ArchiveMode.Read, opts))
                        {
                            var files = zr.ListFilesWithSize(null).ToArray();
                            Console.WriteLine("Found {0} files in archive, testing read-ability for each",
                                files.Length);

                            for (var i = 0; i < files.Length; i++)
                            {
                                Console.Write("Opening file #{0} - {1}", i + 1, files[i].Key);
                                try
                                {
                                    using (var ms = new MemoryStream())
                                    using (var sr = zr.OpenRead(files[i].Key))
                                    {
                                        sr.CopyTo(ms);

                                        if (ms.Length == files[i].Value)
                                            Console.WriteLine(" -- success");
                                        else
                                            Console.WriteLine(" -- bad length: {0} vs {1}", ms.Length, files[i].Value);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    errors = true;
                                    Console.WriteLine(" -- failed: {0}", ex);
                                }

                            }
                        }
                    }

                    if (errors)
                    {
                        Console.WriteLine("Found errors while processing file {0}", file);
                        errorcount++;
                    }
					else
                        Console.WriteLine("Processed file {0} with no errors", file);
                }
                catch (Exception ex)
                {
                    errorcount++;
                    Console.WriteLine("Open failed: {0}", ex);
                }
            }

            Console.Write("Processed {0} zip files", filecount);
            if (errorcount == 0)
                Console.WriteLine(" without errors");
            else
                Console.WriteLine(" and found {0} errors", errorcount);
        }
    }
}
