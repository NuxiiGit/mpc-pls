﻿Imports System.IO

Imports Id3

Public Class Genre
    Implements PlaylistManager.Builder.ICommand

    Public Sub Execute(ByRef list As Playlist, ByVal ParamArray params As String()) Implements PlaylistManager.Builder.ICommand.Execute
        Console.WriteLine("Matching genres...")
        Dim i As Integer = 0
        While i < list.Count
            Dim keep As Boolean = False
            Dim record As String = list(i)
            Select Path.GetExtension(record).ToLower()
            Case ".mp3"
                Using mp3 As New Mp3(record)
                    For Each tag As Id3Tag In mp3.GetAllTags()
                        Dim genres As String() = tag.Genre.ToString().Split("/"c)
                        If params.All(Function(ByVal x As String) genres.Contains(x))
                            keep = True
                            Exit for
                        End If
                    Next
                End Using
            End Select
            If Not keep
                list.RemoveAt(i)
            Else
                i += 1
            End If
        End While
    End Sub

End Class