import { log } from "./logger.js";

/**
 * Background script for a Chrome extension to add URLs as bookmarks in a folder
 *
 * To use in your extension:
 * 1. Add this as a background script in your manifest.json:
 *    "background": {
 *      "service_worker": "background.js"
 *    }
 * 2. Add the bookmarks permission in manifest.json:
 *    "permissions": ["bookmarks"]
 */

/**
 * Validates a URL string and attempts to fix it by adding http:// if needed
 * @param {string} urlString - URL to validate
 * @returns {string|false} - Fixed valid URL string or false if invalid
 */
function isValidUrl(urlString) {
  // Try with the original string
  try {
    const url = new URL(urlString);
    if (url.protocol === "http:" || url.protocol === "https:") {
      return url.href;
    }
  } catch {}

  // Try adding http:// prefix if no protocol
  if (!urlString.match(/^[a-zA-Z]+:\/\//)) {
    try {
      const url = new URL(`http://${urlString}`);
      return url.href;
    } catch {}
  }

  return false;
}

/**
 * Finds a bookmark folder by name, optionally within a specific parent folder
 * @param {string} folderName - Name of the folder to find
 * @param {string} [parentId] - Optional parent folder ID to search within
 * @returns {Promise<object|null>} - Bookmark folder object or null if not found
 */
async function findBookmarkFolder(folderName, parentId = null) {
  // Search for the folder by name
  const results = await chrome.bookmarks.search({ title: folderName });

  for (const bookmark of results) {
    // Get more details about this bookmark item
    const node = await chrome.bookmarks.get(bookmark.id);

    // Check if it's a folder (no URL) and matches parent if specified
    if (node[0] && !node[0].url) {
      // If parentId is specified, check if this folder is a child of that parent
      if (parentId && node[0].parentId !== parentId) {
        continue;
      }
      return node[0]; // Return the folder
    }
  }

  return null; // Folder not found
}

/**
 * Creates a bookmark folder if it doesn't exist
 * @param {string} folderName - Name of the folder to create
 * @param {string} [parentId] - Optional parent folder ID (defaults to Bookmarks Bar)
 * @returns {Promise<object>} - Bookmark folder object
 */
async function createBookmarkFolder(folderName, parentId = null) {
  // Try to find the folder first
  const existingFolder = await findBookmarkFolder(folderName, parentId);
  if (existingFolder) {
    return existingFolder;
  }

  // Determine parent folder ID if not provided
  if (!parentId) {
    // Get the Bookmarks Bar folder ID
    const bookmarksTree = await chrome.bookmarks.getTree();
    parentId = bookmarksTree[0].children[0].id; // Bookmarks bar is the first child
  }

  // Create the folder
  return chrome.bookmarks.create({
    parentId: parentId,
    title: folderName,
  });
}

/**
 * Checks if a bookmark with the given URL already exists in the specified folder
 * @param {string} folderId - ID of the folder to check in
 * @param {string} url - URL to check for
 * @returns {Promise<boolean|object>} - Returns false if not found, or the bookmark object if found
 */
async function bookmarkExists(folderId, url) {
  // Get all children of the folder
  const children = await chrome.bookmarks.getChildren(folderId);

  // Normalize the URL for comparison
  const normalizedUrl = url.toLowerCase().replace(/\/$/, "");

  // Look for a matching URL
  for (const child of children) {
    if (child.url) {
      const childUrl = child.url.toLowerCase().replace(/\/$/, "");
      if (childUrl === normalizedUrl) {
        return child; // Return the existing bookmark
      }
    }
  }

  return false;
}

/**
 * Adds a list of URLs as bookmarks in a folder
 * @param {string} folderName - Name of the folder to add bookmarks to
 * @param {Array<{url: string, title: string}|string>} urlList - List of URL objects or strings
 * @param {string} [parentId] - Optional parent folder ID
 * @returns {Promise<object>} - Result object with success status and created bookmarks
 */
export async function addUrlsAsBookmarks(folderName, urlList, parentId = null) {
  try {
    // Create or get the folder
    const folder = await createBookmarkFolder(folderName, parentId);

    // Add each URL as a bookmark
    for (const item of urlList) {
      // Extract URL and title, with fallbacks
      const url = isValidUrl(typeof item === "string" ? item : item.url);
      if (!url) continue;

      // Use the title from the object or fallback to the hostname
      const title = typeof item === "string" ? new URL(url).hostname : item.title;

      try {
        // Check if the bookmark already exists
        const existing = await bookmarkExists(folder.id, url);

        if (existing && existing.title !== title) {
          // Update the existing bookmark if title is different
          await chrome.bookmarks.update(existing.id, { title });
        } else if (!existing) {
          // Create a new bookmark
          await chrome.bookmarks.create({
            parentId: folder.id,
            title,
            url,
          });
        }
      } catch (err) {
        log.error(`Error adding bookmark for ${url}: ${err.message}`);
      }
    }

    return {
      success: true,
      folderId: folder.id,
      folderName: folder.title,
    };
  } catch (error) {
    return {
      success: false,
      message: `Error adding bookmarks: ${error.message}`,
    };
  }
}

//   // Example of how to Listen for messages from the popup or content script
//   chrome.runtime.onMessage.addListener((message, sender, sendResponse) => {
//     if (message.action === "addBookmarks") {
//       // Process the request asynchronously
//       addUrlsAsBookmarks(message.folderName, message.urlList, message.parentId)
//         .then(result => sendResponse(result))
//         .catch(error => sendResponse({
//           success: false,
//           message: `Error: ${error.message}`
//         }));

//       // Return true to indicate we'll respond asynchronously
//       return true;
//     }
//   });

// Example of how to call from popup.js or content script:
/*
  chrome.runtime.sendMessage({
    action: 'addBookmarks',
    folderName: 'My Favorite Sites',
    urlList: [
      { url: 'https://example.com', title: 'Example Site' },
      'https://google.com',
      { url: 'https://developer.chrome.com', title: 'Chrome Developer Docs' }
    ]
  }, (response) => {
    if (response && response.success) {
      console.log(`Added ${response.added} bookmarks!`);
    } else {
      console.error('Error adding bookmarks:', response?.message);
    }
  });
  */
