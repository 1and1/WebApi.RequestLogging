﻿using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace RequestLoggingSample.Controllers
{
    public class SampleController : ApiController
    {
        [HttpGet, Route("success")]
        public string GetSuccess()
        {
            return "Some content";
        }

        [HttpPost, Route("success")]
        public string PostSuccess([FromBody] string input)
        {
            return "Some content";
        }

        [HttpDelete, Route("success")]
        public void DeleteSuccess()
        {
        }

        [HttpGet, Route("fail")]
        public void GetFail()
        {
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Mock error"));
        }

        [HttpPost, Route("fail")]
        public void PostFail([FromBody] string input)
        {
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Mock error"));
        }

        [HttpDelete, Route("fail")]
        public void DeleteFail()
        {
            throw new HttpResponseException(Request.CreateErrorResponse(HttpStatusCode.Conflict, "Mock error"));
        }
    }
}