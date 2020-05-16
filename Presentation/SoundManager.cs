using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Media;

using PrgResources = DDictionary.Properties.Resources;


namespace DDictionary.Presentation
{
    /// <summary>
    /// Manager to download and play clauses' sounds.
    /// </summary>
    public static class SoundManager
    {
        //HACK: SoundManager should be a singleton...

        //https://www.wpf-tutorial.com/audio-video/playing-audio/
        private static readonly MediaPlayer mediaPlayer = new MediaPlayer { Volume = 1 };


        /// <summary>Folder to place downloaded sounds' files.</summary>
        public static readonly DirectoryInfo SoundsCacheFolder = new DirectoryInfo(".\\sndCache");


        /// <summary>
        /// Try to download clause's sound if it's necessary and play it.
        /// </summary>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. It shouldn't be a local file.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        /// <seealso cref="DDictionary.Presentation.SoundManager.UpdateSoundCache"/>
        /// <exception cref="System.IO.FileNotFoundException"/>
        /// <exception cref="System.IO.IOException"/>
        public static async Task PlaySoundAsync(int clauseId, string soundUri, string dictionary)
        {
            if(String.IsNullOrEmpty(dictionary))
                throw new ArgumentNullException(nameof(dictionary));

            try
            {
                if(String.IsNullOrEmpty(soundUri))
                { //There is no sound for this clause
                    Debug.WriteLine($"Broken link in {nameof(SoundManager)}.{nameof(PlaySoundAsync)}()!");
                    return;
                }

                Uri source = await UpdateSoundCache(clauseId, soundUri, dictionary, overwriteCache: false);

                //Play sound file
                if(mediaPlayer.Source != source)
                    mediaPlayer.Open(source);

                mediaPlayer.Stop(); //To stop previous play
                mediaPlayer.Play();
            }
            catch(Exception ex) when(!(ex is FileNotFoundException))
            {
                throw new IOException(
                    String.Format(PrgResources.CannotPlaySound, soundUri, ex.Message), ex);
            }
        }

        /// <summary>
        /// Stop playing.
        /// </summary>
        public static void StopPlaying()
        {
            mediaPlayer.Stop();
            mediaPlayer.Close();
        }

        /// <summary>
        /// Download sound to the local cache if it's not there or update it if 
        /// <paramref name="overwriteCache"/> is true.
        /// </summary>
        /// <remarks>For local files it only checks whether the file in place or not.</remarks>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. It shouldn't be a local file.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        /// <param name="overwriteCache">Update file in the cache.</param>
        /// <returns>Uri to the local file.</returns>
        /// <exception cref="System.ArgumentNullException"/>
        /// <exception cref="System.IO.FileNotFoundException"/>
        public static async Task<Uri> UpdateSoundCache(int clauseId, string soundUri, string dictionary, bool overwriteCache)
        {
            if(String.IsNullOrEmpty(soundUri))
                throw new ArgumentNullException(nameof(soundUri));

            if(String.IsNullOrEmpty(dictionary))
                throw new ArgumentNullException(nameof(dictionary));


            var source = new Uri(soundUri);

            if(source.IsAbsoluteUri && !source.IsFile)
            { //Let's try to download this file
                if(!SoundsCacheFolder.Exists)
                    SoundsCacheFolder.Create();

                var localFile = new FileInfo(
                    Path.Combine(SoundsCacheFolder.FullName, MakeCachedFileName(clauseId, source, dictionary)));

                if(!localFile.Exists || overwriteCache)
                { //It's not in the cache yet or need to update
                    using(var client = new System.Net.WebClient())
                    {
                        try { await client.DownloadFileTaskAsync(source, localFile.FullName); }
                        catch
                        {
                            localFile.Delete(); //Wrong file in the cache
                            throw;
                        }
                    }
                }

                source = new Uri(localFile.FullName); //Now it's path to local cached file
            }

            if(!File.Exists(source.LocalPath))
                throw new FileNotFoundException(
                    String.Format(PrgResources.FileNotFoundError, source.LocalPath), source.LocalPath);

            return source;
        }

        /// <summary>
        /// Remove cached file.
        /// It removes <b>only downloaded sounds' caches</b> not local files (like c:\myfile.mp3).
        /// </summary>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. Local file will be skipped. Empty string or <c>null</c> as well.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        public static void RemoveFromCache(int clauseId, string soundUri, string dictionary)
        {
            if(String.IsNullOrEmpty(dictionary))
                throw new ArgumentNullException(nameof(dictionary));


            if(String.IsNullOrEmpty(soundUri))
                return; //There is no link

            var source = new Uri(soundUri);

            if(!source.IsAbsoluteUri || source.IsFile)
                return; //Local file

            var localFile = new FileInfo(
                Path.Combine(SoundsCacheFolder.FullName, MakeCachedFileName(clauseId, source, dictionary)));

            if(localFile.Exists)
                localFile.Delete();
        }

        /// <summary>
        /// Is there file in cache for this clause.
        /// </summary>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. It shouldn't be a local file.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        /// <param name="fullName">Full name of the local file in the cache.</param>
        public static bool IsFileCached(int clauseId, string soundUri, string dictionary, out string fullName)
        {
            if(String.IsNullOrEmpty(dictionary))
                throw new ArgumentNullException(nameof(dictionary));


            fullName = null;

            if(String.IsNullOrEmpty(soundUri))
                return false;

            try
            {
                var source = new Uri(soundUri);

                if(!source.IsAbsoluteUri || source.IsFile)
                    return false;

                var localFile =
                    new FileInfo(Path.Combine(SoundsCacheFolder.FullName, MakeCachedFileName(clauseId, source, dictionary)));

                if(localFile.Exists)
                    fullName = localFile.FullName;

                return localFile.Exists;
            }
            catch { return false; } //Something wrong with Uri
        }

        /// <summary>
        /// Try to update clause's sound in the cache.
        /// </summary>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. It shouldn't be a local file.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        /// <seealso cref="DDictionary.Presentation.SoundManager.UpdateSoundCache"/>
        /// <exception cref="System.IO.FileNotFoundException"/>
        /// <exception cref="System.IO.IOException"/>
        public static async Task TryRefreshAsync(int clauseId, string soundUri, string dictionary)
        {
            if(String.IsNullOrEmpty(dictionary))
                throw new ArgumentNullException(nameof(dictionary));


            try
            {
                if(String.IsNullOrEmpty(soundUri))
                { //There is no sound for this clause
                    Debug.WriteLine($"Broken link in {nameof(SoundManager)}.{nameof(TryRefreshAsync)}()!");
                    return;
                }

                await UpdateSoundCache(clauseId, soundUri, dictionary, overwriteCache: true);
            }
            catch(Exception ex) when(!(ex is FileNotFoundException))
            {
                throw new IOException(
                    String.Format(PrgResources.CannotRefreshSound, soundUri, ex.Message), ex);
            }
        }

        /// <summary>
        /// Make an unique name for the sound file in cache (cuz original names could repeat each other).
        /// </summary>
        /// <param name="clauseId">Clause Id.</param>
        /// <param name="soundUri">Sound Uri. It shouldn't be a local file.</param>
        /// <param name="dictionary">Name of the dictionary.</param>
        /// <exception cref="ArgumentNullException"/>
        /// <exception cref="ArgumentException">If <paramref name="soundUri"/> is local file's Uri.</exception>
        public static string MakeCachedFileName(int clauseId, Uri soundUri, string dictionary)
        {
            if(soundUri is null)
                throw new ArgumentNullException(nameof(soundUri));

            if(!soundUri.IsAbsoluteUri || soundUri.IsFile)
                throw new ArgumentException("It shouldn't be file uri.", nameof(soundUri));

            if(dictionary is null)
                throw new ArgumentNullException(nameof(dictionary));


            return String.Concat(
                dictionary.GetHashCode().ToString("x8"),   //The new name consists of the hash of the dictionary
                clauseId.ToString("x8"),                   //the clause id
                soundUri.GetHashCode().ToString("x8"),     //and the hash of the original source path.
                Path.GetExtension(soundUri.LocalPath));    //File extension remains the same.
        }
    }
}
