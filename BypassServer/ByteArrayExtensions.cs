using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TcpGenericServerNET
{
    public static class ByteArrayExtensions
    {
        private static readonly int[] Empty = new int[0];


        public static int[] Locate(this byte[] self, byte[] candidate, int offset = 0, int size = -1)
        {
            if (IsEmptyLocate(self, candidate))
                return Empty;

            var list = new List<int>();

            if (size < 0 || offset + size > self.Length) size = self.Length - offset;

            for (int i = offset; i < offset + size; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                list.Add(i);
            }

            return list.Count == 0 ? Empty : list.ToArray();
        }


        public static int LocateFirst(this byte[] self, byte[] candidate, int offset = 0, int size = -1)
        {
            int pos = -1;

            if (IsEmptyLocate(self, candidate))
                return -1;

            if (size < 0 || offset + size > self.Length) size = self.Length - offset;

            for (int i = offset; i < offset + size; i++)
            {
                if (!IsMatch(self, i, candidate))
                    continue;

                pos = i;
                break;
            }
            return pos;
        }


        private static bool IsMatch(byte[] array, int position, byte[] candidate)
        {
            if (candidate.Length > (array.Length - position))
                return false;

            for (int i = 0; i < candidate.Length; i++)
                if (array[position + i] != candidate[i])
                    return false;

            return true;
        }

        private static bool IsEmptyLocate(byte[] array, byte[] candidate)
        {
            return array == null
                    || candidate == null
                    || array.Length == 0
                    || candidate.Length == 0
                    || candidate.Length > array.Length;
        }

    }

}
