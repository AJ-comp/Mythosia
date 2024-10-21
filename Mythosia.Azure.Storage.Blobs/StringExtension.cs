using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Mythosia.Azure.Storage.Blobs
{
    public static class StringExtension
    {
        public static string ToBlobContainerName(this string input)
        {
            if (string.IsNullOrEmpty(input))
                throw new ArgumentException("Input cannot be null or empty");

            // 1. 첫 번째 문자는 소문자로 변환하고, 나머지 대문자는 '-소문자'로 변환
            string formattedName = Regex.Replace(input, "([A-Z])", "-$1").ToLower();

            // 2. 허용되지 않는 문자 제거(소문자, 숫자, 하이픈만 허용)
            formattedName = Regex.Replace(formattedName, @"[^a-z0-9-]", "-");

            // 3. 하이픈이 연속적으로 나타나는 경우 단일 하이픈으로 축소
            formattedName = Regex.Replace(formattedName, @"-+", "-");

            // 4. 하이픈으로 시작하거나 끝나지 않도록 정리
            formattedName = formattedName.Trim('-');

            // 5. 최소 길이(3자)와 최대 길이(63자) 확인
            if (formattedName.Length < 3)
                formattedName = formattedName.PadRight(3, 'a'); // 최소 길이를 채우기 위해 'a'로 채움

            if (formattedName.Length > 63)
                formattedName = formattedName.Substring(0, 63);

            // 6. 이름이 빈 문자열이 되지 않도록 확인
            if (string.IsNullOrEmpty(formattedName))
                throw new ArgumentException("Invalid name generated. Ensure input meets basic requirements.");

            return formattedName;
        }
    }
}
