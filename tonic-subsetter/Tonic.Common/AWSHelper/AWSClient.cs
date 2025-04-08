using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Tonic.Common.Models;

namespace Tonic.Common.AWSHelper
{
    public class AWSClient
    {

        private const string bucketName = "deidentification-stage";
        //private static readonly RegionEndpoint bucketRegion = RegionEndpoint.USEast1;
        //private static IAmazonS3 s3Client;


        public static async Task UploadFile(string filePath,string sid, string schema,Table table, string path)
        {
            var credentials = new BasicAWSCredentials("PaOBKvElrk@deidentification-stage@pharmacy","8lDzISkPQ8+I4Cy0NV7nB7k+rQ/IuPBCksYQJfZp");

            var config = new AmazonS3Config
            {
                ServiceURL = "http://wdc-ecs-02-dtn.dstcorp.net:9020",
                ForcePathStyle = true
            };
            using var s3Client = new AmazonS3Client(credentials, config);

            try
            {
                // Create a PutObjectRequest to upload the file
                var putRequest = new PutObjectRequest
                {
                    BucketName = bucketName,
                    Key = $"{DateTime.UtcNow.ToString("yyyy-MM-dd")}/AR.I00.TONIC.{schema.Substring(3,3)}.{sid}.{table.TableName}.csv",
                    FilePath = $"{filePath}.csv",
                    ContentType = "text/csv",
                    ChecksumSHA256= ChecksumAlgorithm.SHA256
                };
                // Upload the file to S3
                var response = await s3Client.PutObjectAsync(putRequest);
                Console.WriteLine("File uploaded successfully!");
            }
            catch (AmazonS3Exception e)
            {
                Console.WriteLine("Error encountered on server. Message: '{0}'", e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unknown error encountered. Message: '{0}'", e.Message);
            }
        }

        private static string GenerateSignature(string stringToSign, string secretKey)
        {
            using (HMACSHA1 hmac = new HMACSHA1(Encoding.UTF8.GetBytes(secretKey)))
            {
                // Compute the hash and return it as a Base64 encoded string
                byte[] hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign));
                return Convert.ToBase64String(hashBytes);
            }
        }
    }



}