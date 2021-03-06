#region License

// Copyright 2010 Jeremy Skinner (http://www.jeremyskinner.co.uk)
//  
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at http://github.com/JeremySkinner/git-dot-aspx

#endregion

namespace GitAspx.Controllers {
	using System;
	using System.Web.Mvc;
	using GitAspx.Lib;
	using GitSharp.Core;
	using GitSharp.Core.Transport;

	// Handles project/git-upload-pack and project/git-receive-pack
	public class RpcController : BaseController {
		readonly RepositoryService repositories;

		public RpcController(RepositoryService repositories) {
			this.repositories = repositories;
		}

		[HttpPost]
		public ActionResult UploadPack(string project) {
			return ExecuteRpc(project, Rpc.UploadPack, repository => {
				using (var pack = new UploadPack(repository)) {
					pack.setBiDirectionalPipe(false);
					pack.Upload(Request.InputStream, Response.OutputStream, Response.OutputStream);
				}
			});
		}

		[HttpPost]
		public ActionResult ReceivePack(string project) {
			return ExecuteRpc(project, Rpc.ReceivePack, repository => {
				var pack = new ReceivePack(repository);
				pack.setBiDirectionalPipe(false);
				pack.receive(Request.InputStream, Response.OutputStream, Response.OutputStream);
			});
		}

		ActionResult ExecuteRpc(string project, Rpc rpc, Action<Repository> action) {
			if (!HasAccess(rpc, checkContentType: true)) {
				return new ForbiddenResult();
			}

			Response.ContentType = string.Format("application/x-git-{0}-result", rpc.GetDescription());
			WriteNoCache();

			var repository = repositories.GetRepository(project);

			if (repository == null) {
				return new NotFoundResult();
			}

			using (repository) {
				action(repository);
			}

			return new EmptyResult();
		}
	}
}