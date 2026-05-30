using Snipdeck.Core.Abstractions;
using Snipdeck.Core.Services;

namespace Snipdeck.Core.Tests.Services
{
    public class StorageRelocationServiceTests
    {
        private static string NewTempDir() => Directory.CreateTempSubdirectory("snipdeck-reloc-").FullName;

        [Fact]
        public void Inspect_returns_NoChange_for_the_same_directory()
        {
            var dir = NewTempDir();
            try
            {
                Assert.Equal(StorageChangeOutcome.NoChange, new StorageRelocationService().Inspect(dir, dir));
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Fact]
        public void Inspect_returns_AdoptTarget_when_the_target_already_has_a_store()
        {
            var current = NewTempDir();
            var target = NewTempDir();
            try
            {
                File.WriteAllText(Path.Combine(target, "store.json"), "{}");
                Assert.Equal(StorageChangeOutcome.AdoptTarget, new StorageRelocationService().Inspect(current, target));
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                Directory.Delete(target, recursive: true);
            }
        }

        [Fact]
        public void Inspect_returns_MoveToTarget_when_only_the_current_has_a_store()
        {
            var current = NewTempDir();
            var target = NewTempDir();
            try
            {
                File.WriteAllText(Path.Combine(current, "store.json"), "{}");
                Assert.Equal(StorageChangeOutcome.MoveToTarget, new StorageRelocationService().Inspect(current, target));
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                Directory.Delete(target, recursive: true);
            }
        }

        [Fact]
        public void Inspect_returns_SetEmptyTarget_when_neither_has_a_store()
        {
            var current = NewTempDir();
            var target = NewTempDir();
            try
            {
                Assert.Equal(StorageChangeOutcome.SetEmptyTarget, new StorageRelocationService().Inspect(current, target));
            }
            finally
            {
                Directory.Delete(current, recursive: true);
                Directory.Delete(target, recursive: true);
            }
        }

        [Fact]
        public void Inspect_returns_Invalid_when_the_target_is_nested_inside_the_current()
        {
            var current = NewTempDir();
            try
            {
                var nested = Path.Combine(current, "icons");
                _ = Directory.CreateDirectory(nested);
                Assert.Equal(StorageChangeOutcome.Invalid, new StorageRelocationService().Inspect(current, nested));
                // ...and the reverse nesting (current inside target) is also rejected.
                Assert.Equal(StorageChangeOutcome.Invalid, new StorageRelocationService().Inspect(nested, current));
            }
            finally
            {
                Directory.Delete(current, recursive: true);
            }
        }

        [Fact]
        public void MoveStore_copies_store_and_icons_then_removes_the_originals()
        {
            var current = NewTempDir();
            var target = NewTempDir();
            try
            {
                File.WriteAllText(Path.Combine(current, "store.json"), "{\"snips\":[]}");
                _ = Directory.CreateDirectory(Path.Combine(current, "icons"));
                File.WriteAllText(Path.Combine(current, "icons", "abc.png"), "icon-bytes");
                Directory.Delete(target);

                new StorageRelocationService().MoveStore(current, target);

                Assert.Equal("{\"snips\":[]}", File.ReadAllText(Path.Combine(target, "store.json")));
                Assert.Equal("icon-bytes", File.ReadAllText(Path.Combine(target, "icons", "abc.png")));
                Assert.False(File.Exists(Path.Combine(current, "store.json")));
                Assert.False(Directory.Exists(Path.Combine(current, "icons")));
            }
            finally
            {
                if (Directory.Exists(current)) { Directory.Delete(current, recursive: true); }
                if (Directory.Exists(target)) { Directory.Delete(target, recursive: true); }
            }
        }
    }
}
