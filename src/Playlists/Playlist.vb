﻿Imports System.IO.StreamReader
Imports System.IO.StreamWriter
Imports System.IO
Imports System.Reflection
Imports System.Runtime.CompilerServices

''' <summary>
''' A class which can be used to construct and manage Media Player (Classic) playlist formats.
''' </summary>
Public Class Playlist
    Implements IList(Of String)

    ''' <summary>
    ''' Maintains a relationship between the name of a file extension, and its actual playlist extension <see cref="Playlist.Extension"/>.
    ''' </summary>
    Private Shared extensions As Dictionary(Of String, Extension) = New Dictionary(Of String, Extension)
    
    ''' <summary>
    ''' Stores the list of filepaths for this playlist.
    ''' </summary>
    Private paths As List(Of String) = New List(Of String)

    ''' <summary>
    ''' Stores whether the playlist should be saved relative to the filepath, or absolute against the root directories.
    ''' </summary>
    Public relative As Boolean = False

    ''' <summary>
    ''' Gets and sets a filepath by index.
    ''' </summary>
    ''' <param name="index">The index to lookup.</param>
    ''' <returns>A filepath found under this index.</returns>
    Default Public Property Item(ByVal index As Integer) As String Implements IList(Of String).Item
        Get
            Return paths(index)
        End Get
        Set(ByVal filepath As String)
            paths(index) = Path.GetFullPath(filepath)
        End Set
    End Property

    ''' <summary>
    ''' Gets the number of filepaths in this playlist.
    ''' </summary>
    ''' <returns>The size of <c>paths</c></returns>
    Public ReadOnly Property Count As Integer Implements ICollection(Of String).Count
        Get
            Return paths.Count
        End Get
    End Property

    ''' <summary>
    ''' Returns whether this collection is ReadOnly.
    ''' </summary>
    ''' <remarks>It is not.</remarks>
    ''' <returns><c>True</c> if this collection is ReadOnly and <c>False</c> otherwise.</returns>
    Public ReadOnly Property IsReadOnly As Boolean Implements ICollection(Of String).IsReadOnly
        Get
            Return False
        End Get
    End Property

    ''' <summary>
    ''' An interface which manages playlist extensions.
    ''' </summary>
    Public Interface Extension
        
        ''' <summary>
        ''' Captures the paths from this stream and inserts them into a <c>String()</c>.
        ''' </summary>
        ''' <param name="stream">The input stream for this file.</param>
        ''' <exception cref="IOException">Thrown if there was a problem decoding the file.</exception>
        Function Decode(ByRef stream As StreamReader) As String()

        ''' <summary>
        ''' Pushes the paths from the <paramref name="paths"/> array into this stream.
        ''' </summary>
        ''' <param name="stream">The output stream for this file.</param>
        ''' <param name="paths">The array of filepaths to encode.</param>
        ''' <exception cref="IOException">Thrown if there was a problem encoding the file.</exception>
        Sub Encode(ByRef stream As StreamWriter, ByRef paths As String())

    End Interface

    ''' <summary>
    ''' Returns an array of all supported extensions.
    ''' </summary>
    ''' <returns>An array of all available extensions.</returns>
    Public Shared Function GetExtensions() As String()
        Return extensions.Keys.ToArray()
    End Function

    ''' <summary>
    ''' Uses reflection to compile the dictionary of file extensions at runtime.
    ''' </summary>
    Shared Sub New()
        Dim template As Type = GetType(Extension)
        For Each dataType As Type In template.Assembly.GetTypes()
            If dataType.IsClass() AndAlso dataType.GetInterfaces.Contains(template) Then _
                    extensions.Add("." & dataType.Name.ToUpper(), Activator.CreateInstance(dataType))
        Next
    End Sub

    ''' <summary>
    ''' Default constructor.
    ''' </summary>
    ''' <remarks>Does nothing.</remarks>
    Public Sub New() : End Sub

    ''' <summary>
    ''' Constructs a playlist from a playlist file.
    ''' </summary>
    ''' <param name="location">The path to consider.</param>
    Public Sub New(ByVal location As String)
        Collect(location)
    End Sub

    ''' <summary>
    ''' Constructs a playlist from an <c>IEnumerable</c> of filepaths.
    ''' </summary>
    ''' <param name="enumerator">An <c>IEnumerable</c> of filepaths.</param>
    Public Sub New(ByVal enumerator As IEnumerable(Of String))
        Add(enumerator.ToArray())
    End Sub

    ''' <summary>
    ''' Constructs a playlist from an array of filepaths.
    ''' </summary>
    ''' <param name="paths">An array of filepaths.</param>
    Public Sub New(ByVal paths As String())
        Add(paths)
    End Sub

    ''' <summary>
    ''' Either loads a playlist file, or searches for constructs a new playlist from a directory of files.
    ''' </summary>
    ''' <param name="location">The path to consider.</param>
    Public Sub Collect(ByVal location As String)
        If File.Exists(Path.GetFullPath(location))
            Load(location)
        Else
            Populate(location)
        End If
    End Sub

    ''' <summary>
    ''' Loads the contents of a playlist file into the playlist.
    ''' </summary>
    ''' <param name="filepath">The path of the playlist file.</param>
    ''' <exception cref="IOException">Thrown when there was an error loading the file contents.</exception>
    ''' <exception cref="KeyNotFoundException">Thrown when the file extension for <paramref name="filepath"/> is not supported.</exception>
    Public Sub Load(ByVal filepath As String)
        filepath = Path.GetFullPath(filepath)
        If Not File.Exists(filepath) Then Throw New IOException("Playlist file does not exist")
        Using input As StreamReader = My.Computer.FileSystem.OpenTextFileReader(filepath)
            If input.EndOfStream Then Throw New IOException("Playlist file cannot be empty")
            ' compile paths
            Dim ext As String = Path.GetExtension(filepath).ToUpper()
            If Not extensions.ContainsKey(ext) Then Throw New _
                    KeyNotFoundException("Cannot load playlist with an unsupported file type '" & ext & "'")
            For Each record As String In extensions(ext).Decode(input)
                paths.Add(Path.GetFullPath(record))
            Next
        End Using
    End Sub

    ''' <summary>
    ''' Loads all possible sound file formats into the playlist.
    ''' </summary>
    ''' <param name="root">The root directory to check for sound files.</param>
    Public Sub Populate(ByVal root As String)
        root = Path.GetFullPath(root)
        If Not Directory.Exists(root) Then Throw New IOException("Top-level directory does not exist")
        Dim dirs As Queue(Of String) = New Queue(Of String)()
        dirs.Enqueue(root)
        While dirs.Count > 0
            Dim dir As String = dirs.Dequeue()
            ' append files at this level
            For Each filename As String In Directory.GetFiles(dir)
                If {".MP3", ".WAV"}.Contains(Path.GetExtension(filename).ToUpper()) Then Add(filename)
            Next
            ' add additional directories
            For Each subDir As String In Directory.GetDirectories(dir)
                dirs.Enqueue(subDir)
            Next
        End While
    End Sub
    
    ''' <summary>
    ''' Encodes the contents of the this playlist into a valid playlist file.
    ''' </summary>
    ''' <param name="filepath">The path of the playlist file.</param>
    ''' <exception cref="KeyNotFoundException">Thrown when the file extension for <paramref name="filepath"/> is not supported.</exception>
    Public Sub Save(ByVal filepath As String)
        filepath = Path.GetFullPath(filepath)
        Dim outputPaths As String() = paths.ToArray()
        If relative
            ' convert the paths to be relative to "filepath"
            Dim delimiter As String = Path.DirectorySeparatorChar
            Dim root As String = Path.GetDirectoryName(filepath)
            outputPaths = outputPaths.Select(Function(ByVal x As String)
                Dim dir As String = root
                Dim backtracks As String = "."
                Do
                    If dir Is Nothing Then Exit Do
                    If x.Contains(dir)
                        x = x.Replace(dir, backtracks)
                        If x IsNot Nothing AndAlso x(0) = delimiter Then _
                                x = x.Remove(0, 1)
                        Exit Do
                    End If
                    dir = Path.GetDirectoryName(dir)
                    backtracks &= delimiter & ".."
                Loop
                Return x
            End Function).ToArray()
        End If
        If Not File.Exists(filepath)
            Dim dir = Path.GetDirectoryName(filepath)
            If Not Directory.Exists(dir) Then _
                    My.Computer.FileSystem.CreateDirectory(dir)
        End If
        Using output As StreamWriter = My.Computer.FileSystem.OpenTextFileWriter(filepath, false)
            Dim ext As String = Path.GetExtension(filepath).ToUpper()
            If Not extensions.ContainsKey(ext) Then Throw New _
                    KeyNotFoundException("Cannot save playlist as an unsupported file type '" & ext & "'")
            extensions(ext).Encode(output, outputPaths)
        End Using
    End Sub

    ''' <summary>
    ''' Shuffles the contents of this playlist.
    ''' </summary>
    Public Sub Shuffle()
        Dim rand As Random = New Random()
        Dim count As Integer = paths.Count
        For i As Integer = 0 To (count - 1)
            Dim j As Integer = rand.Next(i, count)
            If i <> j
                Dim temp As String = paths(i)
                paths(i) = paths(j)
                paths(j) = temp
            End If
        Next
    End Sub

    ''' <summary>
    ''' Adds a filepath to this playlist, if it does not exist.
    ''' </summary>
    ''' <param name="filepath">The filepath to add.</param>
    Public Sub Add(ByVal filepath As String) Implements ICollection(Of String).Add
        If not paths.Contains(filepath) Then paths.Add(Path.GetFullPath(filepath))
    End Sub

    ''' <summary>
    ''' Adds an array of filepaths to this playlist.
    ''' </summary>
    ''' <param name="filepaths">The <c>String()</c> of filepaths to add.</param>
    Public Sub Add(ByVal filepaths As String())
        For Each record As String In filepaths
            Add(record)
        Next
    End Sub

    ''' <summary>
    ''' Inserts a filepath into a specific part of the playlist.
    ''' </summary>
    ''' <param name="index">The index to insert the <paramref name="filepath"/> into.</param>
    ''' <param name="filepath">The filepath of the sound file to insert.</param>
    Public Sub Insert(ByVal index As Integer, ByVal filepath As String) Implements IList(Of String).Insert
        paths.Insert(index, Path.GetFullPath(filepath))
    End Sub

    ''' <summary>
    ''' Removes a specific filepath from the playlist.
    ''' </summary>
    ''' <param name="filepath">The filepath to remove.</param>
    Public Function Remove(ByVal filepath As String) As Boolean Implements ICollection(Of String).Remove
        Return paths.Remove(Path.GetFullPath(filepath))
    End Function
    
    ''' <summary>
    ''' Searches for the index of a specific filepath in the playlist.
    ''' </summary>
    ''' <param name="filepath"></param>
    ''' <returns></returns>
    Public Function IndexOf(ByVal filepath As String) As Integer Implements IList(Of String).IndexOf
        Return paths.IndexOf(Path.GetFullPath(filepath))
    End Function

    ''' <summary>
    ''' Removes a filepath from a specific part of the playlist.
    ''' </summary>
    ''' <param name="index">The index to remove.</param>
    Public Sub RemoveAt(ByVal index As Integer) Implements IList(Of String).RemoveAt
        paths.RemoveAt(index)
    End Sub

    ''' <summary>
    ''' Clears the playlist of its current filepaths.
    ''' </summary>
    Public Sub Clear() Implements ICollection(Of String).Clear
        paths.Clear()
    End Sub

    ''' <summary>
    ''' Implements the iterator for the playlist.
    ''' </summary>
    ''' <returns>An <c>IEnumerator</c> of playlist sound file paths.</returns>
    Public Iterator Function GetEnumerator() As IEnumerator(Of String) Implements IEnumerable(Of String).GetEnumerator
        For Each record As String In paths
            Yield record
        Next
    End Function

    ''' <summary>
    ''' Implements the iterator for the playlist.
    ''' </summary>
    ''' <remarks>Required by interface.</remarks>
    ''' <returns>An <c>IEnumerator</c> of playlist sound file paths.</returns>
    Private Function GetEnumeratorB() As IEnumerator Implements IEnumerable.GetEnumerator
        Return GetEnumerator()
    End Function

    ''' <summary>
    ''' Returns whether the playlist contains a filepath.
    ''' </summary>
    ''' <param name="filepath">The filepath to search for.</param>
    ''' <returns><c>True</c> if the filepath exists and <c>False</c> otherwise.</returns>
    Public Function Contains(ByVal filepath As String) As Boolean Implements ICollection(Of String).Contains
        Return paths.Contains(Path.GetFullPath(filepath))
    End Function

    ''' <summary>
    ''' Converts the playlist into an array.
    ''' </summary>
    ''' <param name="array">The array to copy the playlist filepaths to.</param>
    ''' <param name="arrayIndex">The starting index.</param>
    Public Sub CopyTo(ByVal  array As String(), ByVal arrayIndex As Integer) Implements ICollection(Of String).CopyTo
        For Each record As String In paths
            array(arrayIndex) = record
            arrayIndex += 1
        Next
    End Sub

End Class

''' <summary>
''' A module which implements extension methods for the <c>Playlist</c> class.
''' </summary>
Friend Module PlaylistExtensions
    
    ''' <summary>
    ''' Attaches to the <c>IEnumerable</c> class to expose a usability function for converting to the <c>Playlist</c> type.
    ''' </summary>
    ''' <param name="enumerator">An <c>IEnumerable</c> of filepaths.</param>
    ''' <returns>A playlist.</returns>
    <Extension()> 
    Public Function ToPlaylist(ByVal enumerator As IEnumerable(Of String)) As Playlist
        Return New Playlist(enumerator)
    End Function

End Module