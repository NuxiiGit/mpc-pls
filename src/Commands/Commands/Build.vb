﻿Imports mpc_playlist.Playlist
Imports mpc_playlist.Command
Imports System.IO
Imports Id3

Public Class Build
    Implements Command.Extension

    Public ReadOnly Property Description As String Implements Extension.Description
        Get
            Return "Builds a new playlist from a directory of files." & _
                    Environment.NewLine & Environment.NewLine & "The full syntax of this command is: 'build <destination> <file-mask> [-mask <mask>] [-rating <amount>] [-genres <genres>] [-artists <artists>] [-album <album>] [--append]'" & _
                    Environment.NewLine & Environment.NewLine & "Use 'build <playlist> <file-mask>' to build a playlist containing files relative to the current directory which satisfy the file mask 'file-mask.' Masks follow the usual mask syntax." & _
                    Environment.NewLine & Environment.NewLine & "Use 'build <playlist> <file-mask> -genres <genre>' to build a playlist where each audio file is of a certain genre 'genre.'" & _
                    Environment.NewLine & Environment.NewLine & "Use 'build <playlist> <file-mask> -artists <artist>' to build a playlist where each audio file contans a certain artist 'artist.'"
        End Get
    End Property

    ''' <exception cref="ArgumentException">Thrown when <paramref name="params"/> is empty.</exception>
    Public Sub Execute(ParamArray params() As String) Implements Extension.Execute
        If (params.Length = 0) Then Throw New ArgumentException("You must supply a playlist to update the contents of.")
        If (params.Length = 1) Then Throw New ArgumentException("You must supply a file mask to match files.")
        '' compile a list of file paths which match the file mask
        Dim mask As String = params(1)
        Dim root As String = Directory.GetCurrentDirectory() _
                .Replace("\"c, "/"c)
        While (mask.IndexOf("./") = 0)
            mask = mask.Remove(0, 2) '' remove ./ delimiter
            root = Path.GetDirectoryName(root)
        End While
        Dim files As List(Of String) = New List(Of String)
        Dim dirQueue As Queue(Of String) = New Queue(Of String)()
        dirQueue.Enqueue(root)
        While (dirQueue.Count > 0)
            Dim dir As String = dirQueue.Dequeue()
            '' add additional directories
            For Each subDir In Directory.GetDirectories(dir)
                Console.WriteLine("Sub-directory: " & Path.GetFileName(subDir))
                dirQueue.Enqueue(subDir)
            Next
            '' get files
            For Each file As String In Directory.GetFiles(dir)
                If file Like mask Then files.Add(file)
            Next
        End While
        '' build playlist
        Dim playlist As Playlist = New Playlist()
        Dim attributes As List(Of String) = New List(Of String)(params)
        attributes.RemoveAt(0) '' remove path
        attributes.RemoveAt(0) '' remove mask
        Dim lastAttribute As String = Nothing
        For Each attribute In attributes
            Select attribute
            Case "-genres", "-artists", "--append":
                lastAttribute = attribute
            Case Else:
                Select lastAttribute
                Case "-genres":
                    '' filter out genres
                    For Each file In files.ToArray '' I use .ToArray to create a duplicate enumerable since I modify the files list in the loop
                        Dim genres As String() = Nothing
                        If (Path.GetExtension(file) = ".mp3")
                            Using mp3 As New Mp3(file)
                                For Each tag In mp3.GetAllTags()
                                     genres = tag _
                                            .Genre _
                                            .ToString() _
                                            .Split("/"c)
                                     If (genres IsNot Nothing) Then Exit For
                                Next
                            End Using
                        End If
                        If (genres Is Nothing OrElse Not genres.Contains(attribute)) Then files.Remove(file)
                    Next
                Case "-artists":
                    '' filter out artists
                Case Else:
                    Throw New ArgumentException("Expected one of: '--append,' '-genres,' '-artists.' Got: '" & attribute & ".'")
                End Select
            End Select
        Next
        Dim dest As String = params(0)
        If (lastAttribute = "--append")
            Try 
                playlist.Load(dest)
            Catch e As IO.IOException
                Console.WriteLine("There was an error opening the playlist for append.")
            End Try
        End If
        '' write to the playlist
        For Each file In files
            Console.WriteLine(" - Adding File: " & file)
            playlist.Add(file)
        Next
        playlist.Save(dest, False)
    End Sub

End Class